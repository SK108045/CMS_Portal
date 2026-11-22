using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.Api.Data;
using School.Api.Models;
using School.Api.Services;

namespace School.Api.Controllers;

[Authorize, ApiController, Route("api/documents")]
public class DocumentsController(SchoolDb db, Access access, IConfiguration config, IWebHostEnvironment environment) : ControllerBase
{
    string Root => Path.GetFullPath(config["StoragePath"] ?? "../storage", environment.ContentRootPath);

    async Task<int?> Scope(string entity, int id, string kind)
    {
        if (!await access.CanDocument(entity, id, kind))
            throw new UnauthorizedAccessException("You cannot access evidence for this record.");
        var linked = await access.Find(entity, id) ?? throw new ArgumentException("Linked record not found.");
        return await access.Department(linked);
    }

    [HttpPost, RequestSizeLimit(11000000)]
    public async Task<object> Upload(
        [FromForm] string entity,
        [FromForm] int entityId,
        [FromForm] string kind,
        IFormFile file
    )
    {
        access.RequirePermission("documents.upload");
        var dept = await Scope(entity, entityId, kind);
        if (file.Length == 0 || file.Length > 10000000)
            throw new ArgumentException("Files must be between 1 byte and 10 MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new Dictionary<string, string>
        {
            { ".pdf", "application/pdf" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
        };
        if (!allowed.TryGetValue(ext, out var contentType))
            throw new ArgumentException("Only PDF, JPEG and PNG evidence is allowed.");
        await using var stream = file.OpenReadStream();
        var header = new byte[8];
        var read = await stream.ReadAsync(header);
        var valid =
            ext == ".pdf"
                ? System.Text.Encoding.ASCII.GetString(header, 0, Math.Min(read, 5)) == "%PDF-"
            : ext == ".png"
                ? header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            : header[0] == 255 && header[1] == 216 && header[2] == 255;
        if (!valid)
            throw new ArgumentException("File contents do not match the file extension.");
        stream.Position = 0;
        var folder = kind switch
        {
            "Invoice" => "invoices",
            "Quotation" => "quotations",
            "Delivery Note" => "delivery-notes",
            "Payment Proof" => "payment-proofs",
            "Donation Letter" => "donations",
            "Receipt" => "receipts",
            _ => "other",
        };
        var relative = folder + "/" + Guid.NewGuid().ToString("N") + ext;
        Directory.CreateDirectory(Path.Combine(Root, folder));
        var path = Path.Combine(Root, relative);
        try
        {
            await using (var output = System.IO.File.Create(path))
                await stream.CopyToAsync(output);
            var d = new Document
            {
                Entity = entity,
                EntityId = entityId,
                DepartmentId = dept,
                Kind = kind,
                Name = Path.GetFileName(file.FileName),
                RelativePath = relative,
                ContentType = contentType,
                Size = file.Length,
                CreatedBy = access.UserId,
            };
            db.Documents.Add(d);
            await db.SaveChangesAsync();
            access.Audit("Document attached", entity, entityId, dept, d.Name);
            await db.SaveChangesAsync();
            return new
            {
                d.Id
            };
        }
        catch
        {
            System.IO.File.Delete(path);
            throw;
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Download(int id)
    {
        var d = await db.Documents.FindAsync(id);
        if (d == null)
            return NotFound();
        await Scope(d.Entity, d.EntityId, d.Kind);
        var path = Path.Combine(Root, d.RelativePath);
        if (!System.IO.File.Exists(path))
            return NotFound(
                new
                {
                    error = "The evidence file is missing from storage. Restore it from backup."
                }
            );
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, d.ContentType, d.Name);
    }
}
