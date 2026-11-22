using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using School.Api.Data;
using School.Api.Models;
using School.Api.Services;
using Xunit;
public class PermissionTests
{
    [Theory]
    [InlineData("ADMIN", "users.manage", true)]
    [InlineData("ADMIN", "budgets.override", true)]
    [InlineData("FINANCE", "payments.create", true)]
    [InlineData("FINANCE", "users.manage", false)]
    [InlineData("FINANCE", "departments.manage", false)]
    [InlineData("FINANCE", "requests.approve", false)]
    [InlineData("FINANCE", "budgets.override", false)]
    [InlineData("STOREKEEPER", "inventory.manage", true)]
    [InlineData("STOREKEEPER", "payments.create", false)]
    [InlineData("STOREKEEPER", "budgets.manage", false)]
    [InlineData("STOREKEEPER", "requests.receive", true)]
    [InlineData("DEPARTMENT_HEAD", "requests.create", true)]
    [InlineData("DEPARTMENT_HEAD", "requests.approve", false)]
    [InlineData("STAFF", "requests.create", true)]
    [InlineData("STAFF", "payments.create", false)]
    [InlineData("STAFF", "petty.manage", false)]
    public void PermissionMatrix(string role, string permission, bool expected) => Assert.Equal(expected, RolePermissions.Has(role, permission));
    [Theory]
    [InlineData("ADMIN", "budgets", true)]
    [InlineData("FINANCE", "payments", true)]
    [InlineData("STOREKEEPER", "budgets", false)]
    [InlineData("DEPARTMENT_HEAD", "requests", false)]
    [InlineData("STAFF", "payments", false)]
    [InlineData("AUDITOR", "expenses", true)]
    public void ScopeIsResourceSpecific(string role, string resource, bool expected) => Assert.Equal(expected, ResourcePermissions.All(role, resource));
    [Fact]
    public void AuditorHasNoMutationCapabilities()
    {
        foreach (var resource in ResourcePermissions.Rules.Keys)
            Assert.False(ResourcePermissions.CanWrite("AUDITOR", resource));
        foreach (var p in new[] { "requests.approve", "requests.receive", "documents.upload", "budgets.override", "assets.convert" })
            Assert.False(RolePermissions.Has("AUDITOR", p));
    }
    [Fact] public void EvenAdminCannotWriteAuditThroughCrud() => Assert.False(ResourcePermissions.CanWrite("ADMIN", "audit"));
    [Theory][InlineData("STAFF", true)][InlineData("DEPARTMENT_HEAD", true)][InlineData("FINANCE", false)][InlineData("ADMIN", false)][InlineData("STOREKEEPER", false)][InlineData("AUDITOR", false)] public void RequiredDepartments(string role, bool expected) => Assert.Equal(expected, RolePermissions.NeedsDepartment(role));
    [Fact]
    public void StoresReportsCannotBecomeFinancialReports()
    {
        Assert.True(ResourcePermissions.CanReport("FINANCE", "Purchase Requests"));
        Assert.True(ResourcePermissions.CanReport("STOREKEEPER", "Inventory Stock Report"));
        Assert.False(ResourcePermissions.CanReport("STOREKEEPER", "Budget vs Actual"));
        Assert.False(ResourcePermissions.CanReport("STOREKEEPER", "Supplier Payments"));
        Assert.False(ResourcePermissions.CanReport("FINANCE", "Stock Movement Report"));
    }
    [Fact]
    public void UnsupportedRolesFailClosed()
    {
        Assert.False(RolePermissions.Valid("USER"));
        Assert.False(RolePermissions.Has("SUPERUSER", "payments.create"));
        Assert.Empty(RolePermissions.Effective("unknown"));
    }
    static (Access Access, SchoolDb Db) Context(string role, int? dept = 7)
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42"), new Claim(ClaimTypes.Name, "Test Officer"), new Claim(ClaimTypes.Role, role), new Claim("department", dept?.ToString() ?? "") }, "test")) };
        var db = new SchoolDb(new DbContextOptionsBuilder<SchoolDb>().UseNpgsql("Host=localhost;Database=unused").Options);
        return (new Access(new HttpContextAccessor { HttpContext = http }, db), db);
    }
    [Fact]
    public void DepartmentSpoofFailsAtScopeBoundary()
    {
        var (a, db) = Context("DEPARTMENT_HEAD");
        using (db)
        {
            a.Check("requests", 7);
            Assert.Throws<UnauthorizedAccessException>(() => a.Check("requests", 8));
        }
    }
    [Theory]
    [InlineData("STAFF")]
    [InlineData("DEPARTMENT_HEAD")]
    public void DepartmentRolesCannotApprove(string role)
    {
        var (a, db) = Context(role);
        using (db)
            Assert.DoesNotContain("Approved", a.RequestActions(new PurchaseRequest { DepartmentId = 7, RequestedBy = 42, Status = "Submitted" }));
    }
    [Fact]
    public void StaffCannotEditColleagueRequest()
    {
        var (a, db) = Context("STAFF");
        using (db)
        {
            Assert.True(a.CanEditRequest(new PurchaseRequest { DepartmentId = 7, RequestedBy = 42 }));
            Assert.False(a.CanEditRequest(new PurchaseRequest { DepartmentId = 7, RequestedBy = 43 }));
        }
    }
    [Fact]
    public void DepartmentHeadCanEditColleagueButNotOtherDepartment()
    {
        var (a, db) = Context("DEPARTMENT_HEAD");
        using (db)
        {
            Assert.True(a.CanEditRequest(new PurchaseRequest { DepartmentId = 7, RequestedBy = 43 }));
            Assert.False(a.CanEditRequest(new PurchaseRequest { DepartmentId = 8, RequestedBy = 43 }));
        }
    }
    [Fact]
    public void FinanceCannotApproveOwnRequest()
    {
        var (a, db) = Context("FINANCE");
        using (db)
            Assert.Throws<UnauthorizedAccessException>(() => a.ApproveOwn(42, "requests", 1, 7, "My approval"));
    }
    [Fact]
    public void AdminSelfApprovalRequiresReasonAndIsAudited()
    {
        var (a, db) = Context("ADMIN");
        using (db)
        {
            Assert.Throws<ArgumentException>(() => a.ApproveOwn(42, "requests", 1, 7, ""));
            a.ApproveOwn(42, "requests", 1, 7, "Emergency school supplies");
            Assert.Contains(db.ChangeTracker.Entries<AuditLog>(), e => e.Entity.Action == "Self-approval override");
        }
    }
    [Fact]
    public void PermissionRevisionAndCapabilitiesAreStable()
    {
        Assert.Equal(64, RolePermissions.Revision.Length);
        Assert.False(ResourcePermissions.ClientAccess("AUDITOR")["payments"].Create);
        Assert.True(ResourcePermissions.ClientAccess("FINANCE")["payments"].Create);
    }
}
