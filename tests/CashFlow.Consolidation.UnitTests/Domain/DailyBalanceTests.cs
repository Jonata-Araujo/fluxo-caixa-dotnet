using CashFlow.Consolidation.Domain;
using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.UnitTests.Domain;

public sealed class DailyBalanceTests
{
    private static readonly DateOnly Day = new(2026, 9, 29);
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Empty_ShouldHaveZeroBalance()
    {
        var balance = DailyBalance.Empty(Day);

        Assert.Equal(Day, balance.Date);
        Assert.Equal(0m, balance.TotalCredits);
        Assert.Equal(0m, balance.TotalDebits);
        Assert.Equal(0m, balance.NetBalance);
        Assert.Equal(0, balance.TransactionCount);
    }

    [Fact]
    public void Apply_CreditsAndDebits_ShouldConsolidateNetBalance()
    {
        var balance = DailyBalance.Empty(Day);

        balance.Apply(Movement(TransactionType.Credit, 1000m), Now);
        balance.Apply(Movement(TransactionType.Credit, 250.50m), Now);
        balance.Apply(Movement(TransactionType.Debit, 300.25m), Now);

        Assert.Equal(1250.50m, balance.TotalCredits);
        Assert.Equal(300.25m, balance.TotalDebits);
        Assert.Equal(950.25m, balance.NetBalance);
        Assert.Equal(3, balance.TransactionCount);
        Assert.Equal(Now, balance.UpdatedAt);
    }

    [Fact]
    public void Apply_MoreDebitsThanCredits_ShouldResultInNegativeBalance()
    {
        var balance = DailyBalance.Empty(Day);

        balance.Apply(Movement(TransactionType.Credit, 100m), Now);
        balance.Apply(Movement(TransactionType.Debit, 180m), Now);

        Assert.Equal(-80m, balance.NetBalance);
    }

    [Fact]
    public void Apply_MovementOfAnotherDay_ShouldThrow()
    {
        var balance = DailyBalance.Empty(Day);
        var otherDay = BalanceMovement.Create(Guid.NewGuid(), Day.AddDays(1), TransactionType.Credit, 10m).Value;

        Assert.Throws<InvalidOperationException>(() => balance.Apply(otherDay, Now));
    }

    private static BalanceMovement Movement(TransactionType type, decimal amount) =>
        BalanceMovement.Create(Guid.NewGuid(), Day, type, amount).Value;
}
