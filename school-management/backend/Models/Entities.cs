using System.ComponentModel.DataAnnotations;

namespace School.Api.Models;

public abstract class Record
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
}

public class Department : Record
{
    [MaxLength(160)]
    public string Name { get; set; } = "";
    public string Head { get; set; } = "";
    public bool Active { get; set; } = true;
}

public class User : Record
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Position { get; set; } = "";
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Role { get; set; } = "USER";
    public string PasswordHash { get; set; } = "";
    public bool Active { get; set; } = true;
    public int FailedLogins { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int SessionVersion { get; set; }
}

public class Category : Record
{
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
}

public class Budget : Record
{
    public string Name { get; set; } = "";
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public string Year { get; set; } = "2026";
    public string Term { get; set; } = "";
    public DateOnly StartDate { get; set; } = new(2026, 1, 1);
    public DateOnly EndDate { get; set; } = new(2026, 12, 31);
    public decimal Amount { get; set; }
    public string Notes { get; set; } = "";
}

public class BudgetAdjustment : Record
{
    public int BudgetId { get; set; }
    public Budget? Budget { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
}

public class Supplier : Record
{
    public string Name { get; set; } = "";
    public string ContactPerson { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string KraPin { get; set; } = "";
    public string BankDetails { get; set; } = "";
    public string MpesaDetails { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool Active { get; set; } = true;
}

public class PurchaseRequest : Record
{
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int BudgetId { get; set; }
    public Budget? Budget { get; set; }
    public int RequestedBy { get; set; }
    public string Purpose { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal EstimatedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string Priority { get; set; } = "Normal";
    public DateOnly RequiredBy { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Notes { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public List<RequestItem> Items { get; set; } = [];
}

public class RequestItem : Record
{
    public int PurchaseRequestId { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class ApprovalHistory : Record
{
    public int PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }
    public string Decision { get; set; } = "";
    public string Comment { get; set; } = "";
    public decimal Amount { get; set; }
}

public class Invoice : Record
{
    public int PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string Number { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public class Payment : Record
{
    public int PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "M-Pesa";
    public string Reference { get; set; } = "";
    public string Payee { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int ApprovedBy { get; set; }
}

public class Expense : Record
{
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int BudgetId { get; set; }
    public Budget? Budget { get; set; }
    public decimal Amount { get; set; }
    public string Purpose { get; set; } = "";
    public string ResponsiblePerson { get; set; } = "";
    public string Method { get; set; } = "M-Pesa";
    public string Reference { get; set; } = "";
    public string Payee { get; set; } = "";
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int ApprovedBy { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public class PettyAccount : Record
{
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Name { get; set; } = "";
}

public class PettyTransaction : Record
{
    public int? WithdrawalId { get; set; }
    public PettyTransaction? Withdrawal { get; set; }
    public int AccountId { get; set; }
    public PettyAccount? Account { get; set; }
    public int? BudgetId { get; set; }
    public Budget? Budget { get; set; }
    public decimal Amount { get; set; }
    public string Kind { get; set; } = "Expense";
    public string Purpose { get; set; } = "";
    public string Person { get; set; } = "";
    public string Reference { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int? ApprovedBy { get; set; }
}

public class Income : Record
{
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Source { get; set; } = "";
    public string Type { get; set; } = "Donation";
    public string Donor { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Purpose { get; set; } = "";
    public bool Restricted { get; set; }
    public string Method { get; set; } = "Bank Transfer";
    public string Reference { get; set; } = "";
    public string Notes { get; set; } = "";
    public int? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public decimal Quantity { get; set; }
}

public class StockLocation : Record
{
    public string Name { get; set; } = "";
}

public class InventoryItem : Record
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Category { get; set; } = "Consumables";
    public string Unit { get; set; } = "pieces";
    public decimal ReorderLevel { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int LocationId { get; set; }
    public StockLocation? Location { get; set; }
    public string Notes { get; set; } = "";
}

public class StockMovement : Record
{
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Kind { get; set; } = "Received";
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Source { get; set; } = "Purchase";
    public int? PurchaseRequestId { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }
    public string Condition { get; set; } = "Good";
    public string Notes { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public class Asset : Record
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Equipment";
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Location { get; set; } = "";
    public DateOnly AcquisitionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Source { get; set; } = "Purchase";
    public decimal AcquisitionCost { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string Condition { get; set; } = "Good";
    public string Status { get; set; } = "In use";
    public string SerialNumber { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class Document : Record
{
    public string Entity { get; set; } = "";
    public int EntityId { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Kind { get; set; } = "Receipt";
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
}

public class AuditLog : Record
{
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public int EntityId { get; set; }
    public int? DepartmentId { get; set; }
    public string Description { get; set; } = "";
    public string OldValues { get; set; } = "";
    public string NewValues { get; set; } = "";
}

public class PettyFundingRequest : Record
{
    public int AccountId { get; set; }
    public PettyAccount? Account { get; set; }
    public decimal Amount { get; set; }
    public string Purpose { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string DecisionNotes { get; set; } = "";
    public int? ApprovedBy { get; set; }
    public User? Approver { get; set; }
}
