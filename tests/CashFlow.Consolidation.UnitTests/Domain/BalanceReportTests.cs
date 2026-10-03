using CashFlow.Consolidation.Domain;
using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.UnitTests.Domain;

public sealed class BalanceReportTests
{
    private static readonly DateOnly Day1 = new(2026, 9, 1);
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_ShouldIncludeDaysWithoutMovementAndAccumulateBalance()
    {
        var balances = new[]
        {
            Balance(Day1, credits: 500m, debits: 100m),            // +400
            Balance(Day1.AddDays(2), credits: 50m, debits: 200m)   // -150 (dia 2 sem movimento)
        };

        var report = BalanceReport.Build(Day1, Day1.AddDays(3), openingBalance: 1000m, balances).Value;

        Assert.Equal(4, report.Days.Count);
        Assert.Equal(1000m, report.OpeningBalance);

        Assert.Equal(1400m, report.Days[0].ClosingBalance);
        Assert.Equal(0m, report.Days[1].NetBalance);
        Assert.Equal(1400m, report.Days[1].ClosingBalance);
        Assert.Equal(-150m, report.Days[2].NetBalance);
        Assert.Equal(1250m, report.Days[2].ClosingBalance);
        Assert.Equal(1250m, report.Days[3].ClosingBalance);

        Assert.Equal(550m, report.TotalCredits);
        Assert.Equal(300m, report.TotalDebits);
        Assert.Equal(1250m, report.ClosingBalance);
    }

    [Fact]
    public void Build_SingleDay_ShouldWork()
    {
        var report = BalanceReport.Build(Day1, Day1, 0m, [Balance(Day1, 10m, 3m)]).Value;

        var line = Assert.Single(report.Days);
        Assert.Equal(7m, line.NetBalance);
        Assert.Equal(7m, report.ClosingBalance);
    }

    [Fact]
    public void Build_ShouldIgnoreBalancesOutsideThePeriod()
    {
        var report = BalanceReport.Build(Day1, Day1, 0m, [Balance(Day1.AddDays(-1), 999m, 0m)]).Value;

        Assert.Equal(0m, report.ClosingBalance);
    }

    [Fact]
    public void Build_WithInvertedPeriod_ShouldFail()
    {
        var result = BalanceReport.Build(Day1.AddDays(1), Day1, 0m, []);

        Assert.Equal(ConsolidationErrors.InvalidPeriod, result.Error);
    }

    [Fact]
    public void ValidatePeriod_AboveMaximumDays_ShouldFail()
    {
        var result = BalanceReport.ValidatePeriod(Day1, Day1.AddDays(ConsolidationErrors.MaxReportDays));

        Assert.Equal(ConsolidationErrors.PeriodTooLong, result.Error);
    }

    [Fact]
    public void ValidatePeriod_ExactlyMaximumDays_ShouldSucceed()
    {
        var result = BalanceReport.ValidatePeriod(Day1, Day1.AddDays(ConsolidationErrors.MaxReportDays - 1));

        Assert.True(result.IsSuccess);
    }

    private static DailyBalance Balance(DateOnly date, decimal credits, decimal debits)
    {
        var balance = DailyBalance.Empty(date);
        if (credits > 0)
        {
            balance.Apply(BalanceMovement.Create(Guid.NewGuid(), date, TransactionType.Credit, credits).Value, Now);
        }

        if (debits > 0)
        {
            balance.Apply(BalanceMovement.Create(Guid.NewGuid(), date, TransactionType.Debit, debits).Value, Now);
        }

        return balance;
    }
}
