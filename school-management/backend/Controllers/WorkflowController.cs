using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.Api.Data;
using School.Api.Models;
using School.Api.Services;

namespace School.Api.Controllers;

[Authorize, ApiController, Route("api/workflows")]
public class WorkflowController(SchoolDb db, Access access, Ledger ledger) : ControllerBase
{
    public record Decision(string Status, string Comment, decimal? Amount, int? SupplierId);

    [HttpPost("requests/{id:int}/status")]
    public async Task<object> Status(int id, Decision input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable
        );
        var r =
            await db.Requests.FindAsync(id) ?? throw new ArgumentException("Request not found.");
        await access.RequireRecord("requests", r);
        var old = r.Status;
        var next = input.Status;
        if (!access.RequestActions(r).Contains(next))
            throw new UnauthorizedAccessException("You cannot perform this request transition.");
        var allowed = old switch
        {
            "Draft" or "Returned for Changes" => new[] { "Submitted", "Cancelled" },
            "Submitted" => new[]
            {
                "Under Review",
                "Approved",
                "Rejected",
                "Returned for Changes",
                "Cancelled",
            },
            "Under Review" => new[] { "Approved", "Rejected", "Returned for Changes", "Cancelled" },
            "Approved" => new[] { "Ordered", "Partially Received", "Received", "Cancelled" },
            "Ordered" => new[] { "Partially Received", "Received", "Cancelled" },
            "Partially Received" => new[] { "Received" },
            "Paid" => new[] { "Completed" },
            _ => Array.Empty<string>(),
        };
        if (!allowed.Contains(next))
            throw new ArgumentException("Invalid status transition from " + old + " to " + next);
        if (next is "Rejected" or "Returned for Changes" or "Cancelled")
            Ledger.Required(input.Comment, "Decision reason");
        if (next == "Cancelled" && await db.Payments.AnyAsync(p => p.PurchaseRequestId == id))
            throw new ArgumentException("Paid requests cannot be cancelled.");
        if (next == "Approved")
        {
            access.RequirePermission("requests.approve");
            access.ApproveOwn(r.RequestedBy, "requests", r.Id, r.DepartmentId, input.Comment);
            var amount = input.Amount ?? r.EstimatedAmount;
            Ledger.Positive(amount);
            await ledger.CheckBudget(r.BudgetId, r.DepartmentId, amount, input.Comment);
            r.ApprovedAmount = amount;
            r.SupplierId = input.SupplierId ?? r.SupplierId;
            if (
                !r.SupplierId.HasValue
                || !await db.Suppliers.AnyAsync(s => s.Id == r.SupplierId && s.Active)
            )
                throw new ArgumentException("Choose an active supplier before approval.");
        }
        r.Status = next;
        db.Approvals.Add(
            new ApprovalHistory
            {
                PurchaseRequestId = id,
                Decision = next,
                Amount = r.ApprovedAmount,
                Comment = input.Comment,
                CreatedBy = access.UserId,
            }
        );
        access.Audit("Status changed", "requests", id, r.DepartmentId, input.Comment, old, next);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new
        {
            r.Id,
            r.Status
        };
    }

    public record PaymentInput(
        int PurchaseRequestId,
        int InvoiceId,
        decimal Amount,
        string Method,
        string Reference,
        string Payee,
        DateOnly Date,
        string OverrideReason,
        bool FinalPayment
    );

    [HttpPost("payments")]
    public async Task<object> Pay(PaymentInput i)
    {
        access.RequirePermission("payments.create");
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable
        );
        var r =
            await db.Requests.FindAsync(i.PurchaseRequestId)
            ?? throw new ArgumentException("Request not found.");
        access.Check("payments", r.DepartmentId);
        if (r.Status is not ("Received" or "Partially Received"))
            throw new ArgumentException("Receive goods before payment.");
        var invoice =
            await db.Invoices.FindAsync(i.InvoiceId)
            ?? throw new ArgumentException("Invoice not found.");
        if (invoice.PurchaseRequestId != r.Id)
            throw new ArgumentException("Invoice belongs to another request.");
        Ledger.Positive(i.Amount);
        Ledger.Method(i.Method);
        Ledger.Required(i.Reference, "Payment reference");
        Ledger.Required(i.Payee, "Payee");
        if (
            await db.Payments.AnyAsync(p =>
                p.Method == i.Method && p.Reference == i.Reference.Trim().ToUpperInvariant()
            )
            || await db.Expenses.AnyAsync(p =>
                p.Method == i.Method && p.Reference == i.Reference.Trim().ToUpperInvariant()
            )
        )
            throw new ArgumentException("Duplicate payment reference.");
        var invoicePaid =
            await db
                .Payments.Where(p => p.InvoiceId == i.InvoiceId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
        if (invoicePaid + i.Amount > invoice.Amount)
            throw new ArgumentException("Payment exceeds outstanding invoice balance.");
        var paid =
            await db
                .Payments.Where(p => p.PurchaseRequestId == r.Id)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
        var uncommitted =
            Math.Max(0, paid + i.Amount - r.ApprovedAmount) - Math.Max(0, paid - r.ApprovedAmount);
        await ledger.CheckBudget(r.BudgetId, r.DepartmentId, uncommitted, i.OverrideReason);
        if (paid + i.Amount > r.ApprovedAmount)
        {
            access.RequirePermission("budgets.override");
            Ledger.Required(i.OverrideReason, "Payment approval override reason");
            access.Audit("Payment approval override", "requests", r.Id, r.DepartmentId, i.OverrideReason);
        }
        var approvedBy = await db.Approvals.Where(h => h.PurchaseRequestId == r.Id && h.Decision == "Approved").OrderByDescending(h => h.Id).Select(h => h.CreatedBy).FirstOrDefaultAsync() ?? throw new ArgumentException("The request must have an identifiable approval before payment.");
        if (i.FinalPayment)
        {
            if (r.Status != "Received")
                throw new ArgumentException("Receive all goods before final payment.");
            var allInvoices =
                await db
                    .Invoices.Where(x => x.PurchaseRequestId == r.Id)
                    .SumAsync(x => (decimal?)x.Amount) ?? 0;
            if (paid + i.Amount != allInvoices)
                throw new ArgumentException("Final payment must settle all recorded invoices.");
            r.Status = "Paid";
            r.ApprovedAmount = paid + i.Amount;
        }
        var p = new Payment
        {
            PurchaseRequestId = r.Id,
            InvoiceId = i.InvoiceId,
            Amount = i.Amount,
            Method = i.Method,
            Reference = i.Reference.Trim().ToUpperInvariant(),
            Payee = i.Payee,
            Date = i.Date,
            ApprovedBy = approvedBy,
            CreatedBy = access.UserId,
        };
        db.Payments.Add(p);
        await db.SaveChangesAsync();
        access.Audit(
            "Payment recorded",
            "payments",
            p.Id,
            r.DepartmentId,
            $"KES {i.Amount:N2}; request #{r.Id}; {i.Reference}. {i.OverrideReason}"
        );
        access.Audit(
            "Payment applied",
            "requests",
            r.Id,
            r.DepartmentId,
            $"KES {i.Amount:N2}; final: {i.FinalPayment}"
        );
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new
        {
            p.Id
        };
    }

    public record ConvertInput(
        int DepartmentId,
        string Code,
        string Name,
        string Location,
        decimal AcquisitionCost,
        string Source
    );

    [HttpPost("inventory/{id:int}/asset")]
    public async Task<object> ConvertAsset(int id, ConvertInput input)
    {
        access.RequirePermission("assets.convert");
        access.RequirePermission("inventory.manage");
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable
        );
        var item =
            await db.Inventory.FindAsync(id)
            ?? throw new ArgumentException("Inventory item not found.");
        await access.RequireRecord("inventory", item);
        access.Check("assets", input.DepartmentId);
        if (!access.Has("assets.manage"))
            input = input with
            {
                AcquisitionCost = await db.Movements.Where(m => m.InventoryItemId == item.Id && (m.Kind == "Received" || m.Kind == "Donated")).OrderByDescending(m => m.Id).Select(m => m.UnitCost).FirstOrDefaultAsync()
            };
        if (item.Category != "Permanent Items")
            throw new ArgumentException("Only permanent items can become assets.");
        if (await ledger.Quantity(id) < 1)
            throw new ArgumentException("No stock available to assign as an asset.");
        Ledger.Required(input.Code, "Asset code");
        Ledger.Required(input.Name, "Asset name");
        if (input.AcquisitionCost < 0)
            throw new ArgumentException("Cost cannot be negative.");
        var a = new Asset
        {
            Code = input.Code,
            Name = input.Name,
            DepartmentId = input.DepartmentId,
            Location = input.Location,
            AcquisitionCost = input.AcquisitionCost,
            Source = input.Source,
            Category = item.Category,
            CreatedBy = access.UserId,
        };
        db.Assets.Add(a);
        db.Movements.Add(
            new StockMovement
            {
                InventoryItemId = id,
                DepartmentId = item.DepartmentId,
                Kind = "Issued",
                Quantity = 1,
                Source = "Asset conversion",
                Notes = "Assigned as asset " + input.Code,
                CreatedBy = access.UserId,
            }
        );
        await db.SaveChangesAsync();
        access.Audit(
            "Converted to asset",
            "inventory",
            id,
            item.DepartmentId,
            "One unit assigned to asset #" + a.Id
        );
        access.Audit(
            "Asset assigned",
            "assets",
            a.Id,
            a.DepartmentId,
            "From inventory item #" + id
        );
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new
        {
            a.Id
        };
    }

    public record FundingDecision(bool Approve, string Comment);

    [HttpPost("petty-requests/{id:int}/decision")]
    public async Task<object> Fund(int id, FundingDecision input)
    {
        access.RequirePermission("petty.manage");
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable
        );
        var request =
            await db.PettyRequests.Include(r => r.Account).SingleOrDefaultAsync(r => r.Id == id)
            ?? throw new ArgumentException("Funding request not found.");
        access.Check("petty-requests", request.Account!.DepartmentId);
        access.ApproveOwn(request.CreatedBy ?? 0, "petty-requests", id, request.Account.DepartmentId, input.Comment);
        if (request.Status != "Pending")
            throw new ArgumentException("This funding request has already been decided.");
        Ledger.Required(input.Comment, "Decision comments / cash issue reference");
        request.Status = input.Approve ? "Approved" : "Rejected";
        request.DecisionNotes = input.Comment;
        request.ApprovedBy = access.UserId;
        if (input.Approve)
            db.PettyTransactions.Add(
                new PettyTransaction
                {
                    AccountId = request.AccountId,
                    Amount = request.Amount,
                    Kind = "Replenishment",
                    Purpose = request.Purpose,
                    Person = "Department custodian",
                    Reference = input.Comment,
                    CreatedBy = access.UserId,
                    ApprovedBy = access.UserId,
                }
            );
        access.Audit(
            request.Status,
            "petty-requests",
            id,
            request.Account!.DepartmentId,
            input.Comment
        );
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new
        {
            request.Id,
            request.Status
        };
    }
}
