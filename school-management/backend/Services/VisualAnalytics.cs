using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using School.Api.Data;
namespace School.Api.Services;

// Chart values are calculated on the server from the same authorised rows as the tables.
public class VisualAnalytics(SchoolDb db)
{
    record AccountRef(int DepartmentId, string Name);
    Dictionary<int, string>? departmentNames;
    Dictionary<int, int>? requestDepartments;
    Dictionary<int, AccountRef>? accountDepartments;
    public record Point(string Key, string Label, decimal Value, decimal Secondary = 0, decimal Tertiary = 0);
    public record Month(string MonthKey, decimal Value);
    public record Summary(string ComparisonTitle, string DistributionTitle, string ComparisonUnit,
        string DistributionUnit, string PrimaryLabel, string? SecondaryLabel, string? TertiaryLabel,
        List<Point> Comparison, List<Point> Distribution, List<Month> Trend, int Records);
    static decimal Number(JsonObject row, string field) => row[field]?.GetValue<decimal>() ?? 0;
    static string Text(JsonObject row, string field) => row[field]?.ToString() ?? "";
    static int? Id(JsonObject row, string field) => int.TryParse(Text(row, field), out var id) ? id : null;
    public async Task<Summary> Build(string resource, IEnumerable<JsonObject> source)
    {
        var rows = source.ToList();
        var departments = departmentNames ??= await db.Departments.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        var requests = requestDepartments ??= await db.Requests.AsNoTracking().Select(x => new { x.Id, x.DepartmentId }).ToDictionaryAsync(x => x.Id, x => x.DepartmentId);
        var accounts = accountDepartments ??= await db.PettyAccounts.AsNoTracking().ToDictionaryAsync(x => x.Id, x => new AccountRef(x.DepartmentId, x.Name));
        string Department(JsonObject r)
        {
            var id = Id(r, "departmentId");
            if (id == null && Id(r, "purchaseRequestId") is int q && requests.TryGetValue(q, out var dept))
                id = dept;
            if (id == null && Id(r, "accountId") is int a && accounts.TryGetValue(a, out var account))
                id = account.DepartmentId;
            return id?.ToString() ?? "school";
        }
        string DepartmentName(string key) => int.TryParse(key, out var id) ? departments.GetValueOrDefault(id, "Department #" + id) : "School-wide";
        List<Point> Group(Func<JsonObject, string> key, Func<JsonObject, decimal> value, Func<string, string>? label = null, Func<JsonObject, decimal>? secondary = null, Func<JsonObject, decimal>? tertiary = null) => rows.GroupBy(key).Select(g => new Point(g.Key, label?.Invoke(g.Key) ?? g.Key, g.Sum(value), secondary == null ? 0 : g.Sum(secondary), tertiary == null ? 0 : g.Sum(tertiary))).OrderByDescending(x => x.Value + x.Secondary).ThenBy(x => x.Label).ToList();
        string comparisonTitle = "Records by department", distributionTitle = "Record breakdown", unit = "count", distributionUnit = "count", primary = "Records";
        string? secondary = null, tertiary = null;
        List<Point> comparison, distribution;
        IEnumerable<JsonObject> trendRows = rows.Where(r => r["amount"] != null);
        switch (resource)
        {
            case "budgets":
                comparisonTitle = "Department budget allocation";
                distributionTitle = "Where the budget stands";
                unit = distributionUnit = "money";
                primary = "Spent";
                secondary = "Committed";
                tertiary = "Available";
                comparison = Group(Department, r => Number(r, "spent"), DepartmentName, r => Number(r, "committed"), r => Math.Max(0, Number(r, "remaining")));
                distribution = [new("spent", "Spent", rows.Sum(r => Number(r, "spent"))), new("committed", "Committed", rows.Sum(r => Number(r, "committed"))), new("available", "Available", Math.Max(0, rows.Sum(r => Number(r, "remaining"))))];
                trendRows = [];
                break;
            case "requests":
            case "petty-requests":
                comparisonTitle = resource == "requests" ? "Requested value by department" : "Funding requested by department";
                distributionTitle = "Requests by stage";
                unit = "money";
                primary = "Requested";
                comparison = Group(Department, r => Number(r, resource == "requests" ? "estimatedAmount" : "amount"), DepartmentName);
                if (resource == "requests" && rows.All(r => r["estimatedAmount"] == null))
                {
                    comparisonTitle = "Orders available for receiving";
                    unit = "count";
                    primary = "Orders";
                    comparison = Group(Department, r => 1, DepartmentName);
                }
                distribution = Group(r => Text(r, "status"), r => 1);
                trendRows = [];
                break;
            case "income":
                rows = rows.Where(r => Text(r, "type") != "Non-cash Donation").ToList();
                comparisonTitle = "Cash income by source";
                distributionTitle = "Income mix";
                unit = distributionUnit = "money";
                primary = "Received";
                comparison = Group(r => Text(r, "source"), r => Number(r, "amount"));
                distribution = Group(r => Text(r, "type"), r => Number(r, "amount"));
                trendRows = rows;
                break;
            case "expenses":
            case "payments":
            case "transactions":
                comparisonTitle = "Actual spending by department";
                distributionTitle = "How payments were made";
                unit = distributionUnit = "money";
                primary = "Paid";
                comparison = Group(Department, r => Number(r, "amount"), DepartmentName);
                distribution = Group(r => string.IsNullOrWhiteSpace(Text(r, "method")) ? "Petty Cash" : Text(r, "method"), r => Number(r, "amount"));
                break;
            case "petty-accounts":
                comparisonTitle = "Cash held by department";
                distributionTitle = "Imprest accountability";
                unit = distributionUnit = "money";
                primary = "Cash balance";
                secondary = "Outstanding advances";
                comparison = Group(r => Text(r, "name"), r => Number(r, "balance"), secondary: r => Number(r, "outstandingAdvances"));
                distribution = [new("cash", "Cash balance", rows.Sum(r => Number(r, "balance"))), new("advances", "Outstanding advances", rows.Sum(r => Number(r, "outstandingAdvances")))];
                trendRows = [];
                break;
            case "inventory":
                comparisonTitle = "Stock on hand · item units";
                distributionTitle = "Items by classification";
                unit = "quantity";
                primary = "Quantity";
                comparison = rows.Select(r => new Point(Text(r, "id"), Text(r, "name") + " (" + Text(r, "unit") + ")", Number(r, "quantity"))).OrderByDescending(x => x.Value).ToList();
                distribution = Group(r => Text(r, "category"), r => 1);
                trendRows = [];
                break;
            case "assets":
                comparisonTitle = "Asset investment & maintenance";
                distributionTitle = "Asset condition";
                unit = "money";
                primary = "Acquisition";
                secondary = "Maintenance";
                comparison = rows.Select(r => new Point(Text(r, "id"), Text(r, "name"), Number(r, "acquisitionCost"), Number(r, "maintenanceCost"))).OrderByDescending(x => x.Value + x.Secondary).ToList();
                distribution = Group(r => Text(r, "condition"), r => 1);
                trendRows = [];
                break;
            case "petty-transactions":
                comparisonTitle = "Petty cash activity";
                distributionTitle = "Movement types";
                primary = "Movements";
                comparison = Group(r => Text(r, "kind"), r => 1);
                distribution = comparison;
                trendRows = rows.Where(r => Text(r, "kind") == "Expense");
                break;
            default:
                distributionTitle = resource == "audit" ? "Activity by action" : resource == "movements" ? "Stock movement types" : "Records by type";
                comparisonTitle = "Records by department";
                comparison = Group(Department, r => 1, DepartmentName);
                distribution = Group(r => Text(r, resource == "audit" ? "action" : resource == "movements" ? "kind" : r["status"] != null ? "status" : r["type"] != null ? "type" : "entity"), r => 1);
                trendRows = [];
                break;
        }
        var trend = trendRows.Where(r => Text(r, "date").Length >= 7).GroupBy(r => Text(r, "date")[..7]).Select(g => new Month(g.Key, g.Sum(r => Number(r, "amount")))).OrderBy(x => x.MonthKey).ToList();
        return new(comparisonTitle, distributionTitle, unit, distributionUnit, primary, secondary, tertiary, comparison, distribution.Where(x => x.Value > 0).ToList(), trend, source.Count());
    }
}
