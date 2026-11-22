using School.Api.Services;
using Xunit;

public class FinancialTests
{
    [Fact]
    public void RemainingExcludesCommitmentsAndSpending() =>
        Assert.Equal(800000, FinancialRules.Remaining(2000000, 0, 300000, 900000));

    [Fact]
    public void ApprovalReservesButDoesNotSpend() =>
        Assert.Equal(300000, FinancialRules.Committed("Approved", 300000, 0));

    [Fact]
    public void PartialPaymentReducesCommitment() =>
        Assert.Equal(200000, FinancialRules.Committed("Received", 300000, 100000));

    [Theory]
    [InlineData("Rejected")]
    [InlineData("Cancelled")]
    [InlineData("Completed")]
    public void InactiveRequestsReleaseCommitment(string status) =>
        Assert.Equal(0, FinancialRules.Committed(status, 300000, 0));

    [Fact]
    public void OverpaymentDoesNotCreateNegativeCommitment() =>
        Assert.Equal(0, FinancialRules.Committed("Received", 1000, 1200));

    [Fact]
    public void PettyCashReplenishmentRestoresFloat() =>
        Assert.Equal(
            30000,
            FinancialRules.PettyBalance(
                new[] { ("Opening", 30000m), ("Expense", 2300m), ("Replenishment", 2300m) }
            )
        );

    [Fact]
    public void StockIssuesReduceQuantity() =>
        Assert.Equal(
            17,
            FinancialRules.StockDelta("Received", 20) + FinancialRules.StockDelta("Issued", 3)
        );

    [Theory]
    [InlineData(false, 4, 5, false)]
    [InlineData(false, 4, 4, true)]
    [InlineData(true, 4, 5, true)]
    public void DepartmentScope(bool admin, int user, int record, bool allowed) =>
        Assert.Equal(allowed, FinancialRules.CanAccess(admin, user, record));

    [Fact]
    public void UnassignedUserCannotReadSchoolPrivateRecords() =>
        Assert.False(FinancialRules.CanAccess(false, null, null));

    [Fact]
    public void SettlingWithdrawalDoesNotDebitCashTwice() =>
        Assert.Equal(0, FinancialRules.CashDelta("Expense", 3000, true));

    [Fact]
    public void UnusedCashReturnRestoresFloat() =>
        Assert.Equal(2000, FinancialRules.CashDelta("Return", 2000, true));
}
