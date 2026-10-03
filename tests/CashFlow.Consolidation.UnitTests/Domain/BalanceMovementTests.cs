using CashFlow.Consolidation.Domain;
using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.UnitTests.Domain;

public sealed class BalanceMovementTests
{
    private static readonly DateOnly Day = new(2026, 9, 29);

    [Fact]
    public void Credit_ShouldOnlyAffectCredits()
    {
        var movement = BalanceMovement.Create(Guid.NewGuid(), Day, TransactionType.Credit, 50m).Value;

        Assert.Equal(50m, movement.CreditAmount);
        Assert.Equal(0m, movement.DebitAmount);
    }

    [Fact]
    public void Debit_ShouldOnlyAffectDebits()
    {
        var movement = BalanceMovement.Create(Guid.NewGuid(), Day, TransactionType.Debit, 50m).Value;

        Assert.Equal(0m, movement.CreditAmount);
        Assert.Equal(50m, movement.DebitAmount);
    }

    [Fact]
    public void Create_WithEmptyTransactionId_ShouldFail()
    {
        var result = BalanceMovement.Create(Guid.Empty, Day, TransactionType.Credit, 50m);

        Assert.Equal(ConsolidationErrors.InvalidTransactionId, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveAmount_ShouldFail(int amount)
    {
        var result = BalanceMovement.Create(Guid.NewGuid(), Day, TransactionType.Credit, amount);

        Assert.Equal(ConsolidationErrors.AmountMustBePositive, result.Error);
    }
}
