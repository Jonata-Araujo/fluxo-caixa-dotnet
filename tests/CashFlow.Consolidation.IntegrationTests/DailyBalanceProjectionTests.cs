using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Consolidation.IntegrationTests;

[Collection(ConsolidationApiCollection.Name)]
public sealed class DailyBalanceProjectionTests(ConsolidationApiFixture fixture)
{
    [Fact]
    public async Task TryApply_ShouldCreateAndIncrementDailyBalance()
    {
        var day = ConsolidationApiFixture.UniqueDay();

        await fixture.ApplyAsync(day, TransactionType.Credit, 100m);
        await fixture.ApplyAsync(day, TransactionType.Debit, 30.50m);

        var balance = await ReadAsync(day);
        Assert.Equal(100m, balance!.TotalCredits);
        Assert.Equal(30.50m, balance.TotalDebits);
        Assert.Equal(69.50m, balance.NetBalance);
        Assert.Equal(2, balance.TransactionCount);
    }

    [Fact]
    public async Task TryApply_SameTransactionTwice_ShouldBeIdempotent()
    {
        var day = ConsolidationApiFixture.UniqueDay();
        var transactionId = Guid.NewGuid();

        var first = await fixture.ApplyAsync(day, TransactionType.Credit, 80m, transactionId);
        var second = await fixture.ApplyAsync(day, TransactionType.Credit, 80m, transactionId);

        Assert.True(first);
        Assert.False(second);
        var balance = await ReadAsync(day);
        Assert.Equal(80m, balance!.NetBalance);
        Assert.Equal(1, balance.TransactionCount);
    }

    /// <summary>
    /// Vários consumidores atualizando o MESMO dia em paralelo não podem perder atualizações.
    /// O upsert incremental no banco garante isso sem locks na aplicação.
    /// </summary>
    [Fact]
    public async Task TryApply_ConcurrentMovementsOnSameDay_ShouldNotLoseUpdates()
    {
        var day = ConsolidationApiFixture.UniqueDay();
        const int credits = 100;
        const int debits = 50;

        var tasks = Enumerable.Range(0, credits).Select(_ => fixture.ApplyAsync(day, TransactionType.Credit, 10m))
            .Concat(Enumerable.Range(0, debits).Select(_ => fixture.ApplyAsync(day, TransactionType.Debit, 3m)));

        await Task.WhenAll(tasks);

        var balance = await ReadAsync(day);
        Assert.Equal(credits * 10m, balance!.TotalCredits);
        Assert.Equal(debits * 3m, balance.TotalDebits);
        Assert.Equal(credits + debits, balance.TransactionCount);
    }

    [Fact]
    public async Task TryApply_ConcurrentDuplicates_ShouldApplyExactlyOnce()
    {
        var day = ConsolidationApiFixture.UniqueDay();
        var transactionId = Guid.NewGuid();

        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => fixture.ApplyAsync(day, TransactionType.Credit, 5m, transactionId)));

        Assert.Single(results, applied => applied);
        Assert.Equal(5m, (await ReadAsync(day))!.NetBalance);
    }

    private async Task<CashFlow.Consolidation.Domain.DailyBalance?> ReadAsync(DateOnly day)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IDailyBalanceReader>().GetAsync(day);
    }
}
