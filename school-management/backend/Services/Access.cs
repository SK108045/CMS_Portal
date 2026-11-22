using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using School.Api.Data;
using School.Api.Models;
namespace School.Api.Services;

public class Access(IHttpContextAccessor context, SchoolDb db)
{
    public int UserId => int.Parse(context.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public string Role => context.HttpContext!.User.FindFirstValue(ClaimTypes.Role) ?? "";
    public bool Admin => Role == "ADMIN";
    public int? DepartmentId => int.TryParse(context.HttpContext!.User.FindFirstValue("department"), out var d) ? d : null;
    public bool Has(string permission) => RolePermissions.Has(Role, permission);
    public void RequirePermission(string permission)
    {
        if (!Has(permission))
            throw new UnauthorizedAccessException("Missing permission: " + permission);
    }
    public bool CanRead(string resource) => ResourcePermissions.CanRead(Role, resource);
    public bool All(string resource) => ResourcePermissions.All(Role, resource);
    public void RequireRead(string resource)
    {
        if (!CanRead(resource))
            throw new UnauthorizedAccessException("You cannot view this resource.");
    }
    public void RequireWrite(string resource)
    {
        if (!ResourcePermissions.CanWrite(Role, resource))
            throw new UnauthorizedAccessException("You cannot change this resource.");
    }
    public void Check(string resource, int? department)
    {
        if (!All(resource) && (!DepartmentId.HasValue || DepartmentId != department))
            throw new UnauthorizedAccessException("This record belongs to another department.");
    }
    public void ApproveOwn(int requestedBy, string resource, int id, int? dept, string reason)
    {
        if (requestedBy != UserId)
            return;
        if (!Admin)
            throw new UnauthorizedAccessException("You cannot approve your own request.");
        Ledger.Required(reason, "Self-approval override reason");
        Audit("Self-approval override", resource, id, dept, reason);
    }
    public bool CanEditRequest(PurchaseRequest r) => (Has("requests.edit.department") && (All("requests") || r.DepartmentId == DepartmentId)) || (Has("requests.edit.own") && r.RequestedBy == UserId && r.DepartmentId == DepartmentId);
    public static readonly string[] ReceivingStatuses = ["Approved", "Ordered", "Partially Received", "Received", "Paid", "Completed"];
    public string[] RequestActions(PurchaseRequest r)
    {
        var candidates = r.Status switch
        {
            "Draft" or "Returned for Changes" => new[] { "Submitted", "Cancelled" },
            "Submitted" => ["Under Review", "Approved", "Rejected", "Returned for Changes", "Cancelled"],
            "Under Review" => ["Approved", "Rejected", "Returned for Changes", "Cancelled"],
            "Approved" => ["Ordered", "Partially Received", "Received", "Cancelled"],
            "Ordered" => ["Partially Received", "Received", "Cancelled"],
            "Partially Received" => ["Received"],
            "Paid" => ["Completed"],
            _ => Array.Empty<string>()
        };
        return candidates.Where(next => next switch { "Submitted" => Has("requests.submit") && CanEditRequest(r), "Cancelled" => Has("requests.approve") || (r.Status is "Draft" or "Returned for Changes" or "Submitted" && Has("requests.submit") && CanEditRequest(r)), "Ordered" => Has("requests.order"), "Partially Received" or "Received" => Has("requests.receive"), "Completed" => Has("requests.complete"), _ => Has("requests.approve") }).ToArray();
    }
    public async Task<int?> Department(Record r) => r switch
    {
        Department d => d.Id,
        User u => u.DepartmentId,
        Budget b => b.DepartmentId,
        BudgetAdjustment a => (await db.Budgets.FindAsync(a.BudgetId))?.DepartmentId,
        PurchaseRequest q => q.DepartmentId,
        ApprovalHistory a => (await db.Requests.FindAsync(a.PurchaseRequestId))?.DepartmentId,
        Invoice i => (await db.Requests.FindAsync(i.PurchaseRequestId))?.DepartmentId,
        Payment p => (await db.Requests.FindAsync(p.PurchaseRequestId))?.DepartmentId,
        Expense e => e.DepartmentId,
        PettyAccount a => a.DepartmentId,
        PettyFundingRequest p => (await db.PettyAccounts.FindAsync(p.AccountId))?.DepartmentId,
        PettyTransaction t => (await db.PettyAccounts.FindAsync(t.AccountId))?.DepartmentId,
        Income i => i.DepartmentId,
        InventoryItem i => i.DepartmentId,
        StockMovement m => m.DepartmentId ?? (await db.Inventory.FindAsync(m.InventoryItemId))?.DepartmentId,
        Asset a => a.DepartmentId,
        Document d => d.DepartmentId,
        AuditLog a => a.DepartmentId,
        _ => null
    };
    public async Task<Record?> Find(string resource, int id) => resource switch
    {
        "requests" => await db.Requests.FindAsync(id),
        "budgets" => await db.Budgets.FindAsync(id),
        "adjustments" => await db.Adjustments.FindAsync(id),
        "payments" => await db.Payments.FindAsync(id),
        "invoices" => await db.Invoices.FindAsync(id),
        "expenses" => await db.Expenses.FindAsync(id),
        "petty-transactions" => await db.PettyTransactions.FindAsync(id),
        "petty-requests" => await db.PettyRequests.FindAsync(id),
        "petty-accounts" => await db.PettyAccounts.FindAsync(id),
        "income" => await db.Income.FindAsync(id),
        "inventory" => await db.Inventory.FindAsync(id),
        "movements" => await db.Movements.FindAsync(id),
        "assets" => await db.Assets.FindAsync(id),
        "suppliers" => await db.Suppliers.FindAsync(id),
        "approvals" => await db.Approvals.FindAsync(id),
        "categories" => await db.Categories.FindAsync(id),
        "locations" => await db.Locations.FindAsync(id),
        _ => null
    };
    public async Task<bool> CanReadRecord(string resource, Record record)
    {
        if (!CanRead(resource))
            return false;
        if (record is Document document)
            return await CanDocument(document.Entity, document.EntityId, document.Kind);
        if (record is AuditLog log)
        {
            if (Has("audit.view.all"))
                return true;
            if (!ResourcePermissions.Financial.Contains(log.Entity) && log.Entity is not ("inventory" or "movements" or "assets"))
                return false;
            if (Has("audit.view.financial") && !ResourcePermissions.Financial.Contains(log.Entity))
                return false;
            var linked = await Find(log.Entity, log.EntityId);
            return linked != null && await CanReadRecord(log.Entity, linked);
        }
        if (record is ApprovalHistory approval)
        {
            var request = await db.Requests.FindAsync(approval.PurchaseRequestId);
            return request != null && await CanReadRecord("requests", request);
        }
        if (record is PurchaseRequest q)
        {
            if (Has("requests.view.all"))
                return true;
            if (Has("requests.view.receiving"))
                return ReceivingStatuses.Contains(q.Status);
            return q.DepartmentId == DepartmentId && (Has("requests.view.department") || Has("requests.view.own") && q.RequestedBy == UserId);
        }
        if (record is PettyFundingRequest funding && !All(resource) && !Has("petty.requests.view.department") && funding.CreatedBy != UserId)
            return false;
        if (resource is "categories" or "locations")
            return true;
        if (resource == "suppliers")
            return Has("suppliers.view.basic") || All(resource);
        if (All(resource))
            return true;
        var dept = await Department(record);
        if (resource == "inventory" && dept == null)
            return true; // Central stock is intentionally shared for viewing.
        return DepartmentId.HasValue && DepartmentId == dept;
    }
    public async Task RequireRecord(string resource, Record record)
    {
        if (!await CanReadRecord(resource, record))
            throw new UnauthorizedAccessException("You cannot access this record.");
    }
    public async Task<bool> CanDocument(string resource, int id, string kind)
    {
        if (!Has("documents.view") || resource is not ("requests" or "payments" or "invoices" or "expenses" or "petty-transactions" or "petty-requests" or "income" or "inventory" or "movements" or "assets"))
            return false;
        if (!Has("documents.financial") && !(Has("documents.receiving") && (resource is "requests" or "inventory" or "movements" or "assets") && (kind is "Delivery Note" or "Receiving Document")))
            return false;
        var linked = await Find(resource, id);
        return linked != null && await CanReadRecord(resource, linked);
    }
    public bool CanReport(string name) => ResourcePermissions.CanReport(Role, name);
    public void RequireReport(string name, int? department)
    {
        if (!CanReport(name))
            throw new UnauthorizedAccessException("This report is not permitted.");
        if (department.HasValue)
            Check(ResourcePermissions.Reports[name].Resource, department);
    }
    public void Redact(string resource, JsonObject node)
    {
        if (resource == "requests" && Has("requests.view.receiving") && !Has("requests.view.all"))
        {
            foreach (var key in new[] { "budgetId", "estimatedAmount", "approvedAmount", "paid", "notes" })
                node.Remove(key);
            if (node["items"] is JsonArray items)
                foreach (var item in items)
                    item?.AsObject().Remove("unitPrice");
        }
        if (resource == "suppliers" && !Has("suppliers.view"))
            foreach (var key in new[] { "kraPin", "bankDetails", "mpesaDetails", "notes", "totalPurchases", "totalPayments", "outstanding" })
                node.Remove(key);
        if (resource == "petty-accounts" && !Has("petty.view"))
        {
            node.Remove("balance");
            node.Remove("outstandingAdvances");
        }
        if (resource == "assets" && !Has("expenses.view"))
        {
            node.Remove("maintenanceCost");
            node.Remove("lifetimeCost");
        }
        if (resource == "documents")
            node.Remove("relativePath");
    }
    public void Audit(string action, string entity, int id, int? dept, string description, string old = "", string next = "") => db.Audit.Add(new AuditLog { CreatedBy = UserId, Actor = context.HttpContext!.User.Identity!.Name!, Action = action, Entity = entity, EntityId = id, DepartmentId = dept, Description = description, OldValues = old, NewValues = next });
}
