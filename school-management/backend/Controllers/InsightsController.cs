using System.Text;
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

[Authorize, ApiController, Route("api/insights")]
public class InsightsController(
    SchoolDb db,
    Access access,
    Ledger ledger,
    IPasswordHasher<User> hasher,
    VisualAnalytics analytics
) : ControllerBase
{
    async Task<JsonArray> Rows(string resource)
    {
        if (!access.CanRead(resource))
            return new JsonArray();
        var c = new RecordsController(db, access, ledger, hasher)
        {
            ControllerContext = ControllerContext,
        };
        return JsonSerializer
            .SerializeToNode(await c.List(resource), RecordsController.Json)!
            .AsArray();
    }

    [HttpGet("dashboard")]
    public async Task<object> Dashboard()
    {
        var budgets = await Rows("budgets");
        var requests = await Rows("requests");
        var payments = await Rows("payments");
        var expenses = await Rows("expenses");
        var petty = await Rows("petty-accounts");
        var income = await Rows("income");
        var pettyExpenses = (await Rows("petty-transactions")).Where(x => x?["kind"]?.GetValue<string>() == "Expense").Select(x => x!.AsObject()).ToList();
        var spending = payments.Concat(expenses).Select(x => x!.AsObject()).Concat(pettyExpenses).ToList();
        var charts = new
        {
            Budgets = await analytics.Build("budgets", budgets.Select(x => x!.AsObject())),
            Spending = await analytics.Build("transactions", spending),
            Income = await analytics.Build("income", income.Select(x => x!.AsObject())),
            Requests = await analytics.Build("requests", requests.Select(x => x!.AsObject()))
        };
        var inventory = await Rows("inventory");
        var assets = await Rows("assets");
        decimal Sum(JsonArray a, string k) => a.Sum(x => x?[k]?.GetValue<decimal>() ?? 0);
        return new
        {
            Charts = charts,
            OperationalCharts = access.CanRead("inventory") ? await analytics.Build("inventory", inventory.Select(x => x!.AsObject())) : null,
            Operations = new
            {
                InventoryItems = inventory.Count,
                Assets = assets.Count,
                ReceivingRequests = requests.Count(x => Access.ReceivingStatuses.Contains(x?["status"]?.ToString() ?? "")),
                LowStock = inventory.Count(x => (x?["quantity"]?.GetValue<decimal>() ?? 0) <= (x?["reorderLevel"]?.GetValue<decimal>() ?? 0))
            },
            Budget = Sum(budgets, "amount"),
            Committed = Sum(budgets, "committed"),
            Spent = Sum(budgets, "spent"),
            Remaining = Sum(budgets, "remaining"),
            Pending = requests.Count(x =>
                new[] { "Submitted", "Under Review" }.Contains(x?["status"]?.GetValue<string>())
            ),
            MissingEvidence = spending
                .Count(x => x?["missingDocuments"]?.GetValue<bool>() == true),
            PettyCash = Sum(petty, "balance"),
            Donations = income
                .Where(x => x?["type"]?.GetValue<string>() is "Donation" or "Grant")
                .Sum(x => x?["amount"]?.GetValue<decimal>() ?? 0),
            OtherIncome = income
                .Where(x => x?["type"]?.GetValue<string>() == "Other Income")
                .Sum(x => x?["amount"]?.GetValue<decimal>() ?? 0),
            Budgets = budgets,
            RecentRequests = requests.Reverse().Take(5),
            RecentTransactions = payments
                .Concat(expenses)
                .OrderByDescending(x => x?["createdAt"]?.GetValue<string>())
                .Take(6),
            Trend = payments
                .Concat(expenses)
                .GroupBy(x => (x?["date"]?.GetValue<string>() ?? "")[..7])
                .Select(g => new
                {
                    Month = g.Key,
                    Amount = g.Sum(x => x?["amount"]?.GetValue<decimal>() ?? 0),
                })
                .OrderBy(x => x.Month),
        };
    }

    [HttpGet("charts/{resource}")]
    public async Task<object> Charts(string resource)
    {
        if (!new[] { "budgets", "requests", "expenses", "payments", "income", "inventory", "assets", "petty-accounts", "petty-transactions", "petty-requests" }.Contains(resource))
            throw new ArgumentException("No visual summary is available for this module.");
        access.RequireRead(resource);
        return await analytics.Build(resource, (await Rows(resource)).Select(x => x!.AsObject()));
    }

    [HttpGet("report-charts")]
    public async Task<object> ReportCharts(string name = "Budget vs Actual", string? from = null, string? to = null, int? departmentId = null, int? categoryId = null, int? supplierId = null, string? status = null, string? search = null)
    {
        var result = await Report(name, from, to, departmentId, categoryId, supplierId, status, search, false) as OkObjectResult;
        var rows = (List<JsonObject>)result!.Value!;
        return await analytics.Build(name == "Transaction Report" ? "transactions" : Reports[name], name == "Transaction Report" ? rows.Where(r => r["type"] == null && (r["kind"] == null || r["kind"]?.ToString() == "Expense")) : rows);
    }

    static readonly Dictionary<string, string> Reports = new()
    {
        ["Budget vs Actual"] = "budgets",
        ["Budget vs Committed vs Spent"] = "budgets",
        ["Department Expenditure"] = "expenses",
        ["Spending by Category"] = "expenses",
        ["Supplier Payments"] = "payments",
        ["Purchase Requests"] = "requests",
        ["Approved / Rejected Requests"] = "requests",
        ["Petty Cash Report"] = "petty-transactions",
        ["Donations / Grants Report"] = "income",
        ["Other Income Report"] = "income",
        ["Missing Documents Report"] = "payments",
        ["Inventory Stock Report"] = "inventory",
        ["Stock Movement Report"] = "movements",
        ["Asset Register"] = "assets",
        ["Asset Maintenance Cost Report"] = "expenses",
        ["Transaction Report"] = "payments",
        ["User Activity Report"] = "audit",
    };

    [HttpGet("reports")]
    public async Task<IActionResult> Report(
        string name = "Budget vs Actual",
        string? from = null,
        string? to = null,
        int? departmentId = null,
        int? categoryId = null,
        int? supplierId = null,
        string? status = null,
        string? search = null,
        bool csv = false
    )
    {
        access.RequireReport(name, departmentId);
        if (!Reports.TryGetValue(name, out var resource))
            throw new ArgumentException("Unknown report.");
        var rows = (await Rows(resource)).Select(x => x!.AsObject()).ToList();
        if (name is "Transaction Report" or "Missing Documents Report")
            rows.AddRange((await Rows("expenses")).Select(x => x!.AsObject()));
        if (name == "Missing Documents Report")
            rows.AddRange(
                (await Rows("petty-transactions"))
                    .Where(x => x?["kind"]?.GetValue<string>() == "Expense")
                    .Select(x => x!.AsObject())
            );
        if (name == "Transaction Report")
        {
            rows.AddRange((await Rows("petty-transactions")).Select(x => x!.AsObject()));
            rows.AddRange((await Rows("income")).Select(x => x!.AsObject()));
        }
        foreach (var r in rows)
        {
            if (r["budgetId"] != null)
            {
                var b = await db.Budgets.FindAsync(r["budgetId"]!.GetValue<int>());
                r["categoryId"] = b?.CategoryId;
            }
            if (r["purchaseRequestId"] != null)
            {
                var q = await db.Requests.FindAsync(r["purchaseRequestId"]!.GetValue<int>());
                r["departmentId"] = q?.DepartmentId;
                r["supplierId"] = q?.SupplierId;
                if (q != null)
                    r["categoryId"] = (await db.Budgets.FindAsync(q.BudgetId))?.CategoryId;
            }
            if (r["accountId"] != null)
                r["departmentId"] = (
                    await db.PettyAccounts.FindAsync(r["accountId"]!.GetValue<int>())
                )?.DepartmentId;
        }
        rows = rows.Where(r =>
            {
                var date = (r["date"] ?? r["createdAt"])?.GetValue<string>() ?? "";
                return (from == null || string.CompareOrdinal(date, from) >= 0)
                    && (
                        to == null
                        || string.CompareOrdinal(date[..Math.Min(10, date.Length)], to) <= 0
                    )
                    && (
                        !departmentId.HasValue || r["departmentId"]?.GetValue<int>() == departmentId
                    )
                    && (!categoryId.HasValue || r["categoryId"]?.GetValue<int>() == categoryId)
                    && (!supplierId.HasValue || r["supplierId"]?.GetValue<int>() == supplierId)
                    && (
                        string.IsNullOrWhiteSpace(status)
                        || r["status"]?.GetValue<string>() == status
                    )
                    && (
                        string.IsNullOrWhiteSpace(search)
                        || r.ToJsonString().Contains(search, StringComparison.OrdinalIgnoreCase)
                    );
            })
            .ToList();
        if (name == "Missing Documents Report")
            rows = rows.Where(r => r["missingDocuments"]?.GetValue<bool>() == true).ToList();
        if (name == "Other Income Report")
            rows = rows.Where(r => r["type"]?.GetValue<string>() == "Other Income").ToList();
        if (name == "Donations / Grants Report")
            rows = rows.Where(r => r["type"]?.GetValue<string>() != "Other Income").ToList();
        if (name == "Asset Maintenance Cost Report")
            rows = rows.Where(r => r["assetId"] != null).ToList();
        if (name == "Approved / Rejected Requests")
            rows = rows.Where(r => r["status"]?.GetValue<string>() is "Approved" or "Rejected")
                .ToList();
        if (!csv)
            return Ok(rows);
        var keys = rows.SelectMany(r => r.Select(x => x.Key))
            .Distinct()
            .Where(k => !rows.Any(r => r[k] is JsonObject or JsonArray))
            .ToList();
        string Escape(string s)
        {
            if (s.Length > 0 && "=+-@\t\r".Contains(s[0]))
                s = "'" + s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
        var output = new StringBuilder("\uFEFF");
        output.AppendLine(string.Join(",", keys.Select(Escape)));
        foreach (var r in rows)
            output.AppendLine(string.Join(",", keys.Select(k => Escape(r[k]?.ToString() ?? ""))));
        return File(
            Encoding.UTF8.GetBytes(output.ToString()),
            "text/csv",
            name.Replace(" / ", "-") + ".csv"
        );
    }
}
