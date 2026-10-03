using CashFlow.Consolidation.Application.Balances.GetDaily;
using CashFlow.Consolidation.Application.Balances.GetReport;
using CashFlow.Consolidation.Domain;
using CashFlow.Consolidation.UnitTests.Fakes;
using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.UnitTests.Application;

public sealed class BalanceQueriesTests
{
    private static readonly DateOnly Day = new(2026, 9, 29);
    private readonly InMemoryDailyBalanceStore _store = new();

    [Fact]
    public async Task GetDaily_DayWithoutMovements_ShouldReturnZeroBalance()
    {
        var result = await new GetDailyBalanceHandler(_store).HandleAsync(new GetDailyBalanceQuery(Day));

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.NetBalance);
        Assert.Equal(0, result.Value.TransactionCount);
        Assert.Null(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task GetDaily_WithMovements_ShouldReturnConsolidatedValues()
    {
        await Apply(Day, TransactionType.Credit, 300m);
        await Apply(Day, TransactionType.Debit, 120m);

        var result = await new GetDailyBalanceHandler(_store).HandleAsync(new GetDailyBalanceQuery(Day));

        Assert.Equal(300m, result.Value.TotalCredits);
        Assert.Equal(120m, result.Value.TotalDebits);
        Assert.Equal(180m, result.Value.NetBalance);
        Assert.Equal(2, result.Value.TransactionCount);
    }

    [Fact]
    public async Task GetReport_ShouldUsePreviousDaysAsOpeningBalance()
    {
        await Apply(Day.AddDays(-10), TransactionType.Credit, 1000m);
        await Apply(Day.AddDays(-1), TransactionType.Debit, 200m);
        await Apply(Day, TransactionType.Credit, 50m);

        var result = await new GetBalanceReportHandler(_store).HandleAsync(new GetBalanceReportQuery(Day.AddDays(-1), Day));

        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, result.Value.OpeningBalance);
        Assert.Equal(800m, result.Value.Days[0].ClosingBalance);
        Assert.Equal(850m, result.Value.ClosingBalance);
    }

    [Fact]
    public async Task GetReport_InvalidPeriod_ShouldFailFastWithoutQueryingStorage()
    {
        var result = await new GetBalanceReportHandler(_store).HandleAsync(new GetBalanceReportQuery(Day, Day.AddDays(-1)));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _store.ReadCount);
    }

    private Task Apply(DateOnly date, TransactionType type, decimal amount) =>
        _store.TryApplyAsync(BalanceMovement.Create(Guid.NewGuid(), date, type, amount).Value);
}
