using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.Api.Data;
using School.Api.Models;
using School.Api.Services;

namespace School.Api.Controllers;

[Authorize, ApiController, Route("api/records")]
public class RecordsController(
    SchoolDb db,
    Access access,
    Ledger ledger,
    IPasswordHasher<User> hasher
) : ControllerBase
{
    internal static readonly Dictionary<string, Type> Types = new()
    {
        ["departments"] = typeof(Department),
        ["users"] = typeof(User),
        ["categories"] = typeof(Category),
        ["budgets"] = typeof(Budget),
        ["adjustments"] = typeof(BudgetAdjustment),
        ["suppliers"] = typeof(Supplier),
        ["requests"] = typeof(PurchaseRequest),
        ["approvals"] = typeof(ApprovalHistory),
        ["invoices"] = typeof(Invoice),
        ["payments"] = typeof(Payment),
        ["expenses"] = typeof(Expense),
        ["petty-accounts"] = typeof(PettyAccount),
        ["petty-transactions"] = typeof(PettyTransaction),
        ["petty-requests"] = typeof(PettyFundingRequest),
        ["income"] = typeof(Income),
        ["locations"] = typeof(StockLocation),
        ["inventory"] = typeof(InventoryItem),
        ["movements"] = typeof(StockMovement),
        ["assets"] = typeof(Asset),
        ["documents"] = typeof(Document),
        ["audit"] = typeof(AuditLog),
    };
    internal static JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
    };

    async Task<List<Record>> All(string resource)
    {
        return resource switch
        {
            "departments" => (await db.Departments.ToListAsync()).Cast<Record>().ToList(),
            "users" => (await db.Users.ToListAsync()).Cast<Record>().ToList(),
            "categories" => (await db.Categories.ToListAsync()).Cast<Record>().ToList(),
            "budgets" => (await db.Budgets.ToListAsync()).Cast<Record>().ToList(),
            "adjustments" => (await db.Adjustments.ToListAsync()).Cast<Record>().ToList(),
            "suppliers" => (await db.Suppliers.ToListAsync()).Cast<Record>().ToList(),
            "requests" => (await db.Requests.Include(x => x.Items).ToListAsync())
                .Cast<Record>()
                .ToList(),
            "approvals" => (await db.Approvals.ToListAsync()).Cast<Record>().ToList(),
            "invoices" => (await db.Invoices.ToListAsync()).Cast<Record>().ToList(),
            "payments" => (await db.Payments.ToListAsync()).Cast<Record>().ToList(),
            "expenses" => (await db.Expenses.ToListAsync()).Cast<Record>().ToList(),
            "petty-requests" => (await db.PettyRequests.ToListAsync()).Cast<Record>().ToList(),
            "petty-accounts" => (await db.PettyAccounts.ToListAsync()).Cast<Record>().ToList(),
            "petty-transactions" => (await db.PettyTransactions.ToListAsync())
                .Cast<Record>()
                .ToList(),
            "income" => (await db.Income.ToListAsync()).Cast<Record>().ToList(),
            "locations" => (await db.Locations.ToListAsync()).Cast<Record>().ToList(),
            "inventory" => (await db.Inventory.ToListAsync()).Cast<Record>().ToList(),
            "movements" => (await db.Movements.ToListAsync()).Cast<Record>().ToList(),
            "assets" => (await db.Assets.ToListAsync()).Cast<Record>().ToList(),
            "documents" => (await db.Documents.ToListAsync()).Cast<Record>().ToList(),
            "audit" => (await db.Audit.OrderByDescending(x => x.Id).ToListAsync())
                .Cast<Record>()
                .ToList(),
            _ => throw new ArgumentException("Unknown collection."),
        };
    }

    internal Task<int?> Department(Record r) => access.Department(r);

    [HttpGet("{resource}")]
    public async Task<object> List(string resource)
    {
        access.RequireRead(resource);
        var result = new List<object>();
        foreach (var r in await All(resource))
        {
            var dept = await Department(r);
            if (!await access.CanReadRecord(resource, r))
                continue;
            var node = JsonSerializer.SerializeToNode(r, r.GetType(), Json)!.AsObject();
            foreach (var field in node.ToList())
                if (field.Value is JsonObject || (field.Value is JsonArray && field.Key != "items"))
                    node.Remove(field.Key);
            node.Remove("passwordHash");
            node.Remove("failedLogins");
            node.Remove("sessionVersion");
            if (r is Budget b)
                node = JsonSerializer
                    .SerializeToNode(await ledger.BudgetSummary(b), Json)!
                    .AsObject();
            if (r is Supplier supplier)
            {
                node["totalPurchases"] =
                    await db
                        .Requests.Where(q => q.SupplierId == supplier.Id)
                        .SumAsync(q => (decimal?)q.ApprovedAmount) ?? 0;
                node["totalPayments"] =
                    await db
                        .Payments.Where(p => p.PurchaseRequest!.SupplierId == supplier.Id)
                        .SumAsync(p => (decimal?)p.Amount) ?? 0;
                var invoiced =
                    await db
                        .Invoices.Where(i => i.SupplierId == supplier.Id)
                        .SumAsync(i => (decimal?)i.Amount) ?? 0;
                node["outstanding"] = invoiced - node["totalPayments"]!.GetValue<decimal>();
            }
            if (r is PettyAccount pa)
            {
                node["balance"] = await ledger.PettyBalance(pa.Id);
                var issued =
                    await db
                        .PettyTransactions.Where(t =>
                            t.AccountId == pa.Id && t.Kind == "Withdrawal"
                        )
                        .SumAsync(t => (decimal?)t.Amount) ?? 0;
                var settled =
                    await db
                        .PettyTransactions.Where(t =>
                            t.AccountId == pa.Id && t.WithdrawalId != null
                        )
                        .SumAsync(t => (decimal?)t.Amount) ?? 0;
                node["outstandingAdvances"] = issued - settled;
            }
            if (r is PettyTransaction advance && advance.Kind == "Withdrawal")
                node["outstanding"] =
                    advance.Amount
                    - (
                        await db
                            .PettyTransactions.Where(t => t.WithdrawalId == advance.Id)
                            .SumAsync(t => (decimal?)t.Amount) ?? 0
                    );
            if (r is InventoryItem item)
                node["quantity"] = await ledger.Quantity(item.Id);
            if (r is Asset a)
            {
                var cost =
                    await db
                        .Expenses.Where(e => e.AssetId == a.Id)
                        .SumAsync(e => (decimal?)e.Amount) ?? 0;
                node["maintenanceCost"] = cost;
                node["lifetimeCost"] = a.AcquisitionCost + cost;
            }
            if (r is PurchaseRequest pr)
            {
                node["paid"] =
                    await db
                        .Payments.Where(p => p.PurchaseRequestId == pr.Id)
                        .SumAsync(p => (decimal?)p.Amount) ?? 0;
            }
            if (r is not School.Api.Models.User && r is not Document && r is not AuditLog)
                node["missingDocuments"] = !await db.Documents.AnyAsync(d =>
                    d.Entity == resource && d.EntityId == r.Id
                );
            if (r is Document doc)
                node["missingFile"] = !System.IO.File.Exists(
                    Path.Combine(StorageRoot(), doc.RelativePath)
                );
            if (r is PurchaseRequest request)
            {
                node["allowedTransitions"] = JsonSerializer.SerializeToNode(access.RequestActions(request));
                node["canEdit"] = access.CanEditRequest(request);
            }
            access.Redact(resource, node);
            result.Add(node);
        }
        return result;
    }

    string StorageRoot() =>
        Path.GetFullPath(
            HttpContext.RequestServices.GetRequiredService<IConfiguration>()["StoragePath"] ?? "../storage",
            HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath
        );

    [HttpPost("{resource}")]
    public Task<object> Create(string resource, JsonElement input) => Save(resource, null, input);

    [HttpPut("{resource}/{id:int}")]
    public Task<object> Edit(string resource, int id, JsonElement input) =>
        Save(resource, id, input);

    async Task<object> Save(string resource, int? id, JsonElement input)
    {
        access.RequireWrite(resource);
        if (!Types.TryGetValue(resource, out var type) || resource is "audit" or "documents" or "approvals" or "payments")
            throw new ArgumentException("Use the dedicated workflow for this record.");
        if (
            id.HasValue
            && resource
                is not (
                    "departments"
                    or "users"
                    or "categories"
                    or "suppliers"
                    or "requests"
                    or "inventory"
                    or "assets"
                    or "locations"
                )
        )
            throw new ArgumentException(
                "Posted financial records are immutable. Use an adjustment or a new transaction."
            );
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable
        );
        var r =
            (Record?)InputBindings.Read(input, type)
            ?? throw new ArgumentException("Invalid record.");
        r.Id = id ?? 0;
        r.CreatedBy = access.UserId;
        r.CreatedAt = DateTime.UtcNow;
        Record? previous = null;
        string old = "";
        if (id.HasValue)
        {
            previous =
                await db.FindAsync(type, id.Value) as Record
                ?? throw new ArgumentException("Record not found.");
            await access.RequireRecord(resource, previous);
            access.Check(resource, await Department(previous));
            if (previous is PurchaseRequest previousRequest && !access.CanEditRequest(previousRequest))
                throw new UnauthorizedAccessException("You cannot edit this request.");
            old = JsonSerializer.Serialize(previous, type, Json);
            r.CreatedAt = previous.CreatedAt;
            r.CreatedBy = previous.CreatedBy;
            db.Entry(previous).State = EntityState.Detached;
        }
        if (r is PurchaseRequest scoped && !access.All("requests"))
            scoped.DepartmentId = access.DepartmentId ?? throw new UnauthorizedAccessException("A department is required.");
        if (resource is not ("requests" or "categories" or "locations" or "suppliers"))
            access.Check(resource, await Department(r));
        var reason = input.TryGetProperty("overrideReason", out var reasonNode)
            ? reasonNode.GetString() ?? ""
            : "";
        switch (r)
        {
            case Department d:
                Ledger.Required(d.Name, "Department name");
                break;
            case Category c:
                Ledger.Required(c.Name, "Category name");
                break;
            case Supplier s:
                Ledger.Required(s.Name, "Supplier name");
                break;
            case User u:
                Ledger.Required(u.Name, "Name");
                Ledger.Required(u.Email, "Email");
                if (!System.Net.Mail.MailAddress.TryCreate(u.Email, out _))
                    throw new ArgumentException("Enter a valid email.");
                u.Email = u.Email.Trim().ToLowerInvariant();
                if (!RolePermissions.Valid(u.Role))
                    throw new ArgumentException("Invalid role.");
                if (RolePermissions.NeedsDepartment(u.Role) && !u.DepartmentId.HasValue)
                    throw new ArgumentException("A department is required.");
                if (
                    u.DepartmentId.HasValue
                    && !await db.Departments.AnyAsync(d => d.Id == u.DepartmentId && d.Active)
                )
                    throw new ArgumentException("Choose an active department.");
                if (previous is User pu)
                {
                    if (pu.Id == access.UserId && (!u.Active || u.Role != "ADMIN"))
                        throw new ArgumentException("You cannot remove your own Admin access.");
                    u.PasswordHash = pu.PasswordHash;
                    u.SessionVersion = pu.SessionVersion + 1;
                    if (pu.Role != u.Role)
                        access.Audit("Role changed", "users", u.Id, u.DepartmentId, "User role changed", pu.Role, u.Role);
                    if (pu.DepartmentId != u.DepartmentId)
                        access.Audit("Department changed", "users", u.Id, u.DepartmentId, "User department changed", pu.DepartmentId?.ToString() ?? "", u.DepartmentId?.ToString() ?? "");
                    if (pu.Active != u.Active)
                        access.Audit(u.Active ? "User activated" : "User deactivated", "users", u.Id, u.DepartmentId, u.Email);
                }
                if (
                    input.TryGetProperty("password", out var pw)
                    && !string.IsNullOrEmpty(pw.GetString())
                )
                {
                    AuthController.ValidatePassword(pw.GetString()!);
                    u.PasswordHash = hasher.HashPassword(u, pw.GetString()!);
                    if (id.HasValue)
                        access.Audit("Password reset", "users", id.Value, u.DepartmentId, u.Email);
                    u.FailedLogins = 0;
                    u.LockedUntil = null;
                }
                if (string.IsNullOrEmpty(u.PasswordHash))
                    throw new ArgumentException("A temporary password is required.");
                break;
            case Budget b:
                Ledger.Required(b.Name, "Budget name");
                Ledger.Positive(b.Amount);
                if (b.EndDate < b.StartDate)
                    throw new ArgumentException("End date must follow start date.");
                break;
            case BudgetAdjustment a:
                Ledger.Required(a.Reason, "Adjustment reason");
                if ((await db.Budgets.FindAsync(a.BudgetId)) == null)
                    throw new ArgumentException("Budget not found.");
                if (await ledger.Available(a.BudgetId) + a.Amount < 0)
                {
                    access.RequirePermission("budgets.override");
                    Ledger.Required(reason, "Budget override reason");
                    access.Audit("Budget override", "budgets", a.BudgetId, await Department(a), reason);
                }
                if (
                    await ledger.Available(a.BudgetId) + a.Amount < 0
                    && string.IsNullOrWhiteSpace(reason)
                )
                    throw new ArgumentException(
                        "Adjustment exceeds available balance; provide override reason."
                    );
                break;
            case PurchaseRequest q:
                access.Check("requests", q.DepartmentId);
                Ledger.Required(q.Purpose, "Purpose");
                if (previous is PurchaseRequest pq)
                {
                    if (pq.Status is not ("Draft" or "Returned for Changes"))
                        throw new ArgumentException(
                            "Only drafts and returned requests can be edited."
                        );
                    q.Status = pq.Status;
                    q.RequestedBy = pq.RequestedBy;
                    foreach (
                        var item in await db
                            .RequestItems.Where(x => x.PurchaseRequestId == q.Id)
                            .ToListAsync()
                    )
                        db.Remove(item);
                }
                else
                {
                    q.Status = "Draft";
                    q.RequestedBy = access.UserId;
                }
                q.ApprovedAmount = 0;
                if (q.Items.Count == 0)
                    throw new ArgumentException("Add at least one request item.");
                foreach (var item in q.Items)
                {
                    item.Id = 0;
                    Ledger.Required(item.Description, "Item description");
                    Ledger.Positive(item.Quantity);
                    Ledger.Positive(item.UnitPrice);
                }
                q.EstimatedAmount = decimal.Round(
                    q.Items.Sum(i => i.Quantity * i.UnitPrice),
                    2,
                    MidpointRounding.AwayFromZero
                );
                var budget = await db.Budgets.FindAsync(q.BudgetId);
                if (
                    budget == null
                    || (
                        budget.DepartmentId != q.DepartmentId
                        && !(access.All("budgets") && budget.DepartmentId == null)
                    )
                )
                    throw new ArgumentException("Choose a department budget.");
                break;
            case Invoice i:
                var req =
                    await db.Requests.FindAsync(i.PurchaseRequestId)
                    ?? throw new ArgumentException("Request not found.");
                await access.RequireRecord("requests", req);
                if (req.Status is not ("Received" or "Partially Received" or "Paid"))
                    throw new ArgumentException("Receive the order before recording its invoice.");
                if (req.SupplierId != i.SupplierId)
                    throw new ArgumentException(
                        "Invoice supplier must match the purchase request."
                    );
                Ledger.Required(i.Number, "Invoice number");
                Ledger.Positive(i.Amount);
                break;
            case Expense e:
                Ledger.Positive(e.Amount);
                Ledger.Required(e.Purpose, "Purpose");
                Ledger.Required(e.ResponsiblePerson, "Responsible person");
                Ledger.Required(e.Reference, "Payment reference");
                e.Reference = e.Reference.Trim().ToUpperInvariant();
                Ledger.Required(e.Payee, "Payee");
                Ledger.Method(e.Method);
                await ledger.CheckBudget(e.BudgetId, e.DepartmentId, e.Amount, reason);
                if (
                    await db.Expenses.AnyAsync(x =>
                        x.Method == e.Method && x.Reference == e.Reference
                    )
                    || await db.Payments.AnyAsync(x =>
                        x.Method == e.Method && x.Reference == e.Reference
                    )
                )
                    throw new ArgumentException("Duplicate payment reference.");
                if (
                    e.AssetId.HasValue
                    && (await db.Assets.FindAsync(e.AssetId))?.DepartmentId != e.DepartmentId
                )
                    throw new ArgumentException("Asset belongs to another department.");
                e.ApprovedBy = access.UserId;
                break;
            case PettyFundingRequest funding:
                var fundingAccount =
                    await db.PettyAccounts.FindAsync(funding.AccountId)
                    ?? throw new ArgumentException("Account not found.");
                access.Check("petty-requests", fundingAccount.DepartmentId);
                Ledger.Positive(funding.Amount);
                Ledger.Required(funding.Purpose, "Purpose");
                funding.Status = "Pending";
                funding.ApprovedBy = null;
                funding.DecisionNotes = "";
                break;
            case PettyAccount a:
                Ledger.Required(a.Name, "Account name");
                break;
            case PettyTransaction t:
                var account =
                    await db.PettyAccounts.FindAsync(t.AccountId)
                    ?? throw new ArgumentException("Account not found.");
                access.Check("petty-transactions", account.DepartmentId);
                Ledger.Positive(t.Amount);
                Ledger.Required(t.Purpose, "Purpose");
                Ledger.Required(t.Person, "Person receiving money");
                if (
                    t.Kind
                    is not ("Opening" or "Replenishment" or "Expense" or "Withdrawal" or "Return")
                )
                    throw new ArgumentException("Invalid petty cash movement.");
                if (t.Kind is "Opening" or "Replenishment")
                    access.RequirePermission("petty.manage");
                if (
                    t.Kind == "Opening"
                    && await db.PettyTransactions.AnyAsync(x => x.AccountId == t.AccountId)
                )
                    throw new ArgumentException("Opening float already exists.");
                if (
                    t.Kind is "Expense" or "Withdrawal"
                    && !t.WithdrawalId.HasValue
                    && t.Amount > await ledger.PettyBalance(t.AccountId)
                )
                    throw new ArgumentException("Insufficient petty cash balance.");
                if (t.Kind == "Return" && !t.WithdrawalId.HasValue)
                    throw new ArgumentException("Select the withdrawal being returned.");
                if (t.WithdrawalId.HasValue)
                {
                    var withdrawal = await db.PettyTransactions.FindAsync(t.WithdrawalId.Value);
                    if (
                        withdrawal == null
                        || withdrawal.AccountId != t.AccountId
                        || withdrawal.Kind != "Withdrawal"
                        || t.Kind is not ("Expense" or "Return")
                    )
                        throw new ArgumentException(
                            "Choose a withdrawal from this account for settlement."
                        );
                    var settled =
                        await db
                            .PettyTransactions.Where(x => x.WithdrawalId == withdrawal.Id)
                            .SumAsync(x => (decimal?)x.Amount) ?? 0;
                    if (settled + t.Amount > withdrawal.Amount)
                        throw new ArgumentException(
                            "Settlement exceeds the outstanding withdrawal."
                        );
                }
                if (t.Kind == "Expense")
                {
                    if (!t.BudgetId.HasValue)
                        throw new ArgumentException("Expense budget required.");
                    await ledger.CheckBudget(
                        t.BudgetId.Value,
                        account.DepartmentId,
                        t.Amount,
                        reason
                    );
                }
                t.ApprovedBy = access.UserId;
                break;
            case Income i:
                Ledger.Required(i.Source, "Income source");
                Ledger.Required(i.Purpose, "Purpose");
                if (i.Restricted)
                    Ledger.Required(i.Purpose, "Restricted purpose");
                if (i.Type == "Non-cash Donation")
                {
                    if (!i.InventoryItemId.HasValue)
                        throw new ArgumentException("Select the donated inventory item.");
                    Ledger.Positive(i.Quantity);
                    var stock =
                        await db.Inventory.FindAsync(i.InventoryItemId.Value)
                        ?? throw new ArgumentException("Inventory item not found.");
                    db.Movements.Add(
                        new StockMovement
                        {
                            InventoryItemId = stock.Id,
                            DepartmentId = stock.DepartmentId,
                            Kind = "Donated",
                            Quantity = i.Quantity,
                            Source = i.Donor,
                            Notes = i.Purpose,
                            CreatedBy = access.UserId,
                        }
                    );
                }
                else
                {
                    Ledger.Positive(i.Amount);
                    Ledger.Method(i.Method);
                }
                break;
            case InventoryItem i:
                Ledger.Required(i.Name, "Item name");
                Ledger.Required(i.Code, "Internal code");
                break;
            case StockMovement m:
                access.RequirePermission("inventory.manage");
                Ledger.Positive(m.Quantity);
                if (
                    !new[]
                    {
                        "Received",
                        "Issued",
                        "Returned",
                        "Adjusted",
                        "Damaged",
                        "Lost",
                        "Transferred",
                        "Donated",
                    }.Contains(m.Kind)
                )
                    throw new ArgumentException("Invalid movement.");
                var inv =
                    await db.Inventory.FindAsync(m.InventoryItemId)
                    ?? throw new ArgumentException("Item not found.");
                if (
                    await ledger.Quantity(m.InventoryItemId)
                        + FinancialRules.StockDelta(m.Kind, m.Quantity)
                    < 0
                )
                    throw new ArgumentException("Insufficient stock.");
                access.Check("inventory", inv.DepartmentId);
                if (m.PurchaseRequestId.HasValue)
                {
                    var purchase = await db.Requests.FindAsync(m.PurchaseRequestId);
                    if (purchase != null)
                        await access.RequireRecord("requests", purchase);
                    if (
                        purchase == null
                        || purchase.Status
                            is not ("Approved" or "Ordered" or "Partially Received" or "Received")
                    )
                        throw new ArgumentException(
                            "Purchase must be approved before stock is received."
                        );
                }
                if (m.Kind == "Transferred")
                {
                    if (m.DepartmentId == null)
                        throw new ArgumentException("Destination department required.");
                    var destination = new InventoryItem
                    {
                        Name = inv.Name,
                        Code = inv.Code + "-T" + Guid.NewGuid().ToString("N")[..6],
                        DepartmentId = m.DepartmentId,
                        LocationId = inv.LocationId,
                        Unit = inv.Unit,
                        Category = inv.Category,
                    };
                    db.Inventory.Add(destination);
                    db.Movements.Add(
                        new StockMovement
                        {
                            InventoryItem = destination,
                            Kind = "Received",
                            Quantity = m.Quantity,
                            DepartmentId = m.DepartmentId,
                            Source = "Transfer from item #" + inv.Id,
                            CreatedBy = access.UserId,
                        }
                    );
                }
                break;
            case Asset a:
                Ledger.Required(a.Name, "Asset name");
                Ledger.Required(a.Code, "Asset code");
                if (a.AcquisitionCost < 0)
                    throw new ArgumentException("Acquisition cost cannot be negative.");
                break;
            case StockLocation l:
                Ledger.Required(l.Name, "Location name");
                break;
        }
        if (id.HasValue)
            db.Update(r);
        else
            db.Add(r);
        await db.SaveChangesAsync();
        access.Audit(
            id.HasValue ? "Updated" : "Created",
            resource,
            r.Id,
            await Department(r),
            resource + " #" + r.Id,
            resource == "users" ? "" : old,
            resource == "users" ? "" : JsonSerializer.Serialize(r, type, Json)
        );
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new
        {
            id = r.Id
        };
    }
}
