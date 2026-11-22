namespace School.Api.Services;

public static class FinancialRules
{
    public static readonly string[] CommittedStatuses =
    [
        "Approved",
        "Ordered",
        "Partially Received",
        "Received",
        "Paid",
    ];

    public static decimal Committed(string status, decimal approved, decimal paid) =>
        CommittedStatuses.Contains(status) ? Math.Max(0, approved - paid) : 0;

    public static decimal Remaining(
        decimal budget,
        decimal adjustments,
        decimal committed,
        decimal spent
    ) => budget + adjustments - committed - spent;

    public static decimal PettyBalance(IEnumerable<(string Kind, decimal Amount)> rows) =>
        rows.Sum(x => x.Kind is "Opening" or "Replenishment" ? x.Amount : -x.Amount);

    public static decimal CashDelta(string kind, decimal amount, bool settlesWithdrawal) =>
        kind == "Expense" && settlesWithdrawal ? 0
        : kind is "Opening" or "Replenishment" or "Return" ? amount
        : -amount;

    public static decimal StockDelta(string kind, decimal qty) =>
        kind is "Issued" or "Damaged" or "Lost" or "Transferred" ? -qty : qty;

    public static bool CanAccess(bool admin, int? userDepartment, int? recordDepartment) =>
        admin || (userDepartment.HasValue && userDepartment == recordDepartment);
}
