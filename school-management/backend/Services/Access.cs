using System.Security.Claims;
using School.Api.Data;
using School.Api.Models;

namespace School.Api.Services;

public class Access(IHttpContextAccessor context, SchoolDb db)
{
    public int UserId =>
        int.Parse(context.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public bool Admin => context.HttpContext!.User.IsInRole("ADMIN");
    public int? DepartmentId =>
        int.TryParse(context.HttpContext!.User.FindFirstValue("department"), out var d) ? d : null;

    public void RequireAdmin()
    {
        if (!Admin)
            throw new UnauthorizedAccessException("Administrator permission is required.");
    }

    public void Check(int? department)
    {
        if (!FinancialRules.CanAccess(Admin, DepartmentId, department))
            throw new UnauthorizedAccessException("This record belongs to another department.");
    }

    public void Audit(
        string action,
        string entity,
        int id,
        int? dept,
        string description,
        string old = "",
        string next = ""
    )
    {
        db.Audit.Add(
            new AuditLog
            {
                CreatedBy = UserId,
                Actor = context.HttpContext!.User.Identity!.Name!,
                Action = action,
                Entity = entity,
                EntityId = id,
                DepartmentId = dept,
                Description = description,
                OldValues = old,
                NewValues = next,
            }
        );
    }
}
