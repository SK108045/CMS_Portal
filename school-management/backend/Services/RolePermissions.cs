using System.Security.Cryptography;
using System.Text;
namespace School.Api.Services;

// This is the single role-to-permission mapping. Controllers depend on permissions, never role names.
public static class RolePermissions
{
    public static readonly string[] Names = ["ADMIN", "FINANCE", "STOREKEEPER", "DEPARTMENT_HEAD", "STAFF", "AUDITOR"];
    static readonly string[] Department = ["departments.view", "categories.view", "budgets.view", "requests.create", "requests.submit", "documents.view", "documents.upload", "documents.financial", "inventory.view", "locations.view", "assets.view", "petty.accounts.view", "petty.requests.view", "petty.request", "reports.view.department", "reports.financial", "reports.stores", "audit.view.department"];
    static readonly string[] Financial = ["departments.view", "departments.view.all", "categories.view", "budgets.view", "budgets.view.all", "requests.view.all", "approvals.view", "suppliers.view", "suppliers.view.all", "invoices.view", "invoices.view.all", "payments.view", "payments.view.all", "expenses.view", "expenses.view.all", "petty.accounts.view", "petty.view", "petty.requests.view", "petty.view.all", "income.view", "income.view.all", "documents.view", "documents.financial", "audit.view.financial", "inventory.view", "inventory.view.all", "locations.view", "assets.view", "assets.view.all"];
    static readonly string[] Stores = ["departments.view", "departments.view.all", "requests.view.receiving", "requests.receive", "suppliers.view.basic", "inventory.view", "inventory.view.all", "inventory.manage", "inventory.movements.view", "locations.view", "locations.manage", "assets.view", "assets.view.all", "assets.convert", "documents.view", "documents.upload", "documents.receiving", "reports.view.all", "reports.stores"];
    static readonly Dictionary<string, string[]> Map = new()
    {
        ["ADMIN"] = ["users.manage", "departments.manage", "budgets.manage", "budgets.override", "requests.approve", "requests.create", "requests.edit.department", "requests.submit", "requests.order", "requests.receive", "requests.complete", "suppliers.manage", "invoices.create", "payments.create", "expenses.create", "petty.manage", "petty.request", "income.create", "inventory.manage", "inventory.movements.view", "locations.manage", "assets.manage", "assets.convert", "documents.upload", "reports.view.all", "reports.financial", "reports.stores", "audit.view.all", "settings.manage", .. Financial, .. Stores],
        ["FINANCE"] = [.. Financial, "suppliers.manage", "invoices.create", "payments.create", "expenses.create", "petty.manage", "petty.request", "income.create", "documents.upload", "requests.order", "requests.complete", "reports.view.all", "reports.financial"],
        ["STOREKEEPER"] = Stores,
        ["DEPARTMENT_HEAD"] = [.. Department, "requests.view.department", "requests.edit.department", "petty.requests.view.department", "approvals.view", "expenses.view", "payments.view", "invoices.view", "petty.view"],
        ["STAFF"] = [.. Department, "requests.view.own", "requests.edit.own", "approvals.view"],
        ["AUDITOR"] = [.. Financial, "requests.view.all", "inventory.movements.view", "reports.view.all", "reports.financial", "reports.stores", "audit.view.all"]
    };
    public static bool Valid(string role) => Map.ContainsKey(role);
    public static bool NeedsDepartment(string role) => role is "STAFF" or "DEPARTMENT_HEAD";
    public static bool Has(string role, string permission) => Valid(role) && (role == "ADMIN" || Map[role].Contains(permission));
    public static string[] Effective(string role) => role == "ADMIN" ? Map.Values.SelectMany(x => x).Distinct().Order(StringComparer.Ordinal).ToArray() : Map.GetValueOrDefault(role, []).Distinct().Order(StringComparer.Ordinal).ToArray();
    // A mapping change revokes existing cookies even without changing individual user records.
    public static string Revision { get; } = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(";", Names.Select(r => r + ":" + string.Join(",", Effective(r)))))));
}

public static class ResourcePermissions
{
    public record Rule(string Read, string? Write = null, string? All = null);
    public static readonly Dictionary<string, Rule> Rules = new()
    {
        ["departments"] = new("departments.view", "departments.manage", "departments.view.all"),
        ["users"] = new("users.manage", "users.manage", "users.manage"),
        ["categories"] = new("categories.view", "budgets.manage"),
        ["budgets"] = new("budgets.view", "budgets.manage", "budgets.view.all"),
        ["adjustments"] = new("budgets.view", "budgets.manage", "budgets.view.all"),
        ["requests"] = new("requests.view.own", "requests.create", "requests.view.all"),
        ["approvals"] = new("approvals.view", null, "requests.view.all"),
        ["suppliers"] = new("suppliers.view", "suppliers.manage", "suppliers.view.all"),
        ["invoices"] = new("invoices.view", "invoices.create", "invoices.view.all"),
        ["payments"] = new("payments.view", "payments.create", "payments.view.all"),
        ["expenses"] = new("expenses.view", "expenses.create", "expenses.view.all"),
        ["petty-accounts"] = new("petty.accounts.view", "petty.manage", "petty.view.all"),
        ["petty-transactions"] = new("petty.view", "petty.manage", "petty.view.all"),
        ["petty-requests"] = new("petty.requests.view", "petty.request", "petty.view.all"),
        ["income"] = new("income.view", "income.create", "income.view.all"),
        ["inventory"] = new("inventory.view", "inventory.manage", "inventory.view.all"),
        ["movements"] = new("inventory.movements.view", "inventory.manage", "inventory.view.all"),
        ["locations"] = new("locations.view", "locations.manage"),
        ["assets"] = new("assets.view", "assets.manage", "assets.view.all"),
        ["documents"] = new("documents.view", "documents.upload"),
        ["audit"] = new("audit.view.department", null, "audit.view.all")
    };
    public static readonly HashSet<string> Financial = ["budgets", "adjustments", "requests", "approvals", "suppliers", "invoices", "payments", "expenses", "petty-accounts", "petty-transactions", "petty-requests", "income"];
    public record ReportRule(string Resource, string Domain);
    public static readonly Dictionary<string, ReportRule> Reports = new()
    {
        ["Budget vs Actual"] = new("budgets", "financial"),
        ["Budget vs Committed vs Spent"] = new("budgets", "financial"),
        ["Department Expenditure"] = new("expenses", "financial"),
        ["Spending by Category"] = new("expenses", "financial"),
        ["Supplier Payments"] = new("payments", "financial"),
        ["Purchase Requests"] = new("requests", "stores"),
        ["Approved / Rejected Requests"] = new("requests", "stores"),
        ["Petty Cash Report"] = new("petty-transactions", "financial"),
        ["Donations / Grants Report"] = new("income", "financial"),
        ["Other Income Report"] = new("income", "financial"),
        ["Missing Documents Report"] = new("payments", "financial"),
        ["Inventory Stock Report"] = new("inventory", "stores"),
        ["Stock Movement Report"] = new("movements", "stores"),
        ["Asset Register"] = new("assets", "stores"),
        ["Asset Maintenance Cost Report"] = new("expenses", "financial"),
        ["Transaction Report"] = new("payments", "financial"),
        ["User Activity Report"] = new("audit", "financial")
    };
    public static bool CanRead(string role, string resource) => Rules.TryGetValue(resource, out var rule) && (RolePermissions.Has(role, rule.Read) || resource switch
    {
        "requests" => RolePermissions.Has(role, "requests.view.department") || RolePermissions.Has(role, "requests.view.all") || RolePermissions.Has(role, "requests.view.receiving"),
        "suppliers" => RolePermissions.Has(role, "suppliers.view.basic"),
        "audit" => RolePermissions.Has(role, "audit.view.all") || RolePermissions.Has(role, "audit.view.financial"),
        _ => false
    });
    public static bool All(string role, string resource) => resource == "audit" && RolePermissions.Has(role,"audit.view.financial") || (resource is "categories" or "locations" && CanRead(role, resource)) || role == "ADMIN" || (Rules.TryGetValue(resource, out var rule) && rule.All != null && RolePermissions.Has(role, rule.All)) || resource == "requests" && RolePermissions.Has(role, "requests.view.receiving");
    public static bool CanWrite(string role, string resource) => Rules.TryGetValue(resource, out var rule) && rule.Write != null && RolePermissions.Has(role, rule.Write);
    public static bool CanReport(string role, string name)
    {
        if (!Reports.TryGetValue(name, out var rule)) return false;
        var reportAccess = RolePermissions.Has(role, "reports.view.all") || RolePermissions.Has(role, "reports.view.department");
        // Purchasing is shared by finance and stores; the underlying record scope still applies.
        var domainAccess = RolePermissions.Has(role, "reports." + rule.Domain)
            || rule.Resource == "requests" && RolePermissions.Has(role, "reports.financial");
        return reportAccess && domainAccess && CanRead(role, rule.Resource);
    }
    public record Capabilities(bool Read, bool Create, bool Edit, bool All);
    public static Dictionary<string, Capabilities> ClientAccess(string role) => Rules.ToDictionary(k => k.Key, k => new Capabilities(CanRead(role, k.Key), CanWrite(role, k.Key), k.Key == "requests" ? RolePermissions.Has(role, "requests.edit.department") || RolePermissions.Has(role, "requests.edit.own") : CanWrite(role, k.Key), All(role, k.Key)));
}
