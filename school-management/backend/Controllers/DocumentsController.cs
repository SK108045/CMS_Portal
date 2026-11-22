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
    string Root => Path.GetFullPath(config["StoragePath"] ?? "../storage",environment.ContentRootPath);

    async Task<int?> Scope(string entity, int id)
    {
        var dept = entity switch
        {
            "requests" => (await db.Requests.FindAsync(id))?.DepartmentId,
            "payments" => await db
                .Payments.Where(p => p.Id == id)
                .Select(p => (int?)p.PurchaseRequest!.DepartmentId)
                .SingleOrDefaultAsync(),
            "invoices" => await db
                .Invoices.Where(p => p.Id == id)
                .Select(p => (int?)p.PurchaseRequest!.DepartmentId)
                .SingleOrDefaultAsync(),
            "expenses" => (await db.Expenses.FindAsync(id))?.DepartmentId,
            "petty-transactions" => await db
                .PettyTransactions.Where(p => p.Id == id)
                .Select(p => (int?)p.Account!.DepartmentId)
                .SingleOrDefaultAsync(),
            "income" => (await db.Income.FindAsync(id))?.DepartmentId,
            "assets" => (await db.Assets.FindAsync(id))?.DepartmentId,
            "inventory" => (await db.Inventory.FindAsync(id))?.DepartmentId,
            _ => throw new ArgumentException("Unsupported document relationship."),
        };
        if (
            !RecordsController.Types.TryGetValue(entity, out var type)
            || await db.FindAsync(type, id) == null
        )
            throw new ArgumentException("Linked record not found.");
        access.Check(dept);
        return dept;
    }

    [HttpPost, RequestSizeLimit(11000000)]
    public async Task<object> Upload(
        [FromForm] string entity,
        [FromForm] int entityId,
        [FromForm] string kind,
        IFormFile file
    )
    {
        var dept = await Scope(entity, entityId);
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
            return new { d.Id };
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
        access.Check(d.DepartmentId);
        var path = Path.Combine(Root, d.RelativePath);
        if (!System.IO.File.Exists(path))
            return NotFound(
                new { error = "The evidence file is missing from storage. Restore it from backup." }
            );
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, d.ContentType, d.Name);
    }
}
