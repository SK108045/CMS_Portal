using Microsoft.EntityFrameworkCore;
using School.Api.Models;

namespace School.Api.Data;

public class SchoolDb(DbContextOptions<SchoolDb> options) : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetAdjustment> Adjustments => Set<BudgetAdjustment>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseRequest> Requests => Set<PurchaseRequest>();
    public DbSet<RequestItem> RequestItems => Set<RequestItem>();
    public DbSet<ApprovalHistory> Approvals => Set<ApprovalHistory>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<PettyAccount> PettyAccounts => Set<PettyAccount>();
    public DbSet<PettyTransaction> PettyTransactions => Set<PettyTransaction>();
    public DbSet<Income> Income => Set<Income>();
    public DbSet<StockLocation> Locations => Set<StockLocation>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<StockMovement> Movements => Set<StockMovement>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditLog> Audit => Set<AuditLog>();

    public DbSet<PettyFundingRequest> PettyRequests => Set<PettyFundingRequest>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        foreach (var e in m.Model.GetEntityTypes())
        {
            foreach (var p in e.GetProperties())
                if (p.ClrType == typeof(decimal))
                    p.SetPrecision(18);
            foreach (var p in e.GetProperties())
                if (p.ClrType == typeof(decimal))
                    p.SetScale(2);
            foreach (var fk in e.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
        foreach (var entity in m.Model.GetEntityTypes().ToList())
        {
            m.Entity(entity.ClrType)
                .HasOne(typeof(User))
                .WithMany()
                .HasForeignKey("CreatedBy")
                .OnDelete(DeleteBehavior.Restrict);
            m.Entity(entity.ClrType).HasIndex("CreatedAt");
        }
        m.Entity<PurchaseRequest>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RequestedBy)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<Payment>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<Expense>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<PettyTransaction>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<PettyFundingRequest>()
            .HasOne(x => x.Approver)
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<User>().HasIndex(x => x.Email).IsUnique();
        m.Entity<Department>().HasIndex(x => x.Name).IsUnique();
        m.Entity<InventoryItem>().HasIndex(x => x.Code).IsUnique();
        m.Entity<Asset>().HasIndex(x => x.Code).IsUnique();
        m.Entity<Payment>().HasIndex(x => new { x.Method, x.Reference }).IsUnique();
        m.Entity<Invoice>().HasIndex(x => new { x.SupplierId, x.Number }).IsUnique();
        m.Entity<PurchaseRequest>().HasIndex(x => new { x.DepartmentId, x.Status });
        m.Entity<Budget>()
            .HasIndex(x => new
            {
                x.DepartmentId,
                x.CategoryId,
                x.Year,
            });
        m.Entity<AuditLog>().HasIndex(x => new { x.Entity, x.EntityId });
        m.Entity<Document>().HasIndex(x => new { x.Entity, x.EntityId });
        m.Entity<Budget>()
            .ToTable(t => t.HasCheckConstraint("CK_Budget_Amount", "\"Amount\" >= 0"));
        m.Entity<Payment>()
            .ToTable(t => t.HasCheckConstraint("CK_Payment_Amount", "\"Amount\" > 0"));
        m.Entity<Expense>()
            .ToTable(t => t.HasCheckConstraint("CK_Expense_Amount", "\"Amount\" > 0"));
    }
}
