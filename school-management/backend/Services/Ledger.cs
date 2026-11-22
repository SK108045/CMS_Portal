using Microsoft.EntityFrameworkCore;
using School.Api.Data;
using School.Api.Models;

namespace School.Api.Services;

public class Ledger(SchoolDb db, Access access)
{
    public async Task<object> BudgetSummary(Budget b)
    {
        var payments =
            await db
                .Payments.Where(p => p.PurchaseRequest!.BudgetId == b.Id)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
        var expenses =
            await db.Expenses.Where(x => x.BudgetId == b.Id).SumAsync(x => (decimal?)x.Amount) ?? 0;
        var petty =
            await db
                .PettyTransactions.Where(x => x.BudgetId == b.Id && x.Kind == "Expense")
                .SumAsync(x => (decimal?)x.Amount) ?? 0;
        var requests = await db.Requests.Where(x => x.BudgetId == b.Id).ToListAsync();
        var paid = await db
            .Payments.Where(x => x.PurchaseRequest!.BudgetId == b.Id)
            .GroupBy(x => x.PurchaseRequestId)
            .Select(g => new { Id = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Id, x => x.Amount);
        var committed = requests.Sum(x =>
            FinancialRules.Committed(x.Status, x.ApprovedAmount, paid.GetValueOrDefault(x.Id))
        );
        var adjustments =
            await db.Adjustments.Where(x => x.BudgetId == b.Id).SumAsync(x => (decimal?)x.Amount)
            ?? 0;
        return new
        {
            b.Id,
            b.Name,
            b.DepartmentId,
            b.CategoryId,
            b.Year,
            b.Term,
            b.StartDate,
            b.EndDate,
            b.Notes,
            Amount = b.Amount + adjustments,
            OriginalAmount = b.Amount,
            Committed = committed,
            Spent = payments + expenses + petty,
            Remaining = FinancialRules.Remaining(
                b.Amount,
                adjustments,
                committed,
                payments + expenses + petty
            ),
        };
    }

    public async Task<decimal> Available(int budgetId)
    {
        var b =
            await db.Budgets.FindAsync(budgetId)
            ?? throw new ArgumentException("Budget not found.");
        var json = System.Text.Json.JsonSerializer.SerializeToElement(await BudgetSummary(b));
        return json.GetProperty("Remaining").GetDecimal();
    }

    public async Task CheckBudget(int budgetId, int department, decimal extra, string reason)
    {
        var b =
            await db.Budgets.FindAsync(budgetId)
            ?? throw new ArgumentException("Budget not found.");
        if (b.DepartmentId != department && !(access.All("budgets") && b.DepartmentId == null))
            throw new ArgumentException("Choose a budget belonging to this department.");
        if (extra > await Available(budgetId))
        {
            access.RequirePermission("budgets.override");
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException(
                    "Amount exceeds available budget. An authorised budget officer must supply an override reason."
                );
            access.Audit("Budget override", "budgets", budgetId, department, reason);
        }
    }

    public async Task<decimal> PettyBalance(int account) =>
        (await db.PettyTransactions.Where(x => x.AccountId == account).ToListAsync()).Sum(x =>
            FinancialRules.CashDelta(x.Kind, x.Amount, x.WithdrawalId.HasValue)
        );

    public async Task<decimal> Quantity(int item) =>
        (await db.Movements.Where(x => x.InventoryItemId == item).ToListAsync()).Sum(x =>
            FinancialRules.StockDelta(x.Kind, x.Quantity)
        );

    public static void Positive(decimal n)
    {
        if (n <= 0 || decimal.Round(n, 2) != n)
            throw new ArgumentException("Enter a positive amount with at most two decimal places.");
    }

    public static void Required(string s, string name)
    {
        if (string.IsNullOrWhiteSpace(s) || s.Length > 4000)
            throw new ArgumentException(name + " is required (maximum 4,000 characters).");
    }

    public static void Method(string m)
    {
        if (!new[] { "M-Pesa", "Bank Transfer", "Cash", "Cheque", "Other" }.Contains(m))
            throw new ArgumentException("Invalid payment method.");
    }
}
