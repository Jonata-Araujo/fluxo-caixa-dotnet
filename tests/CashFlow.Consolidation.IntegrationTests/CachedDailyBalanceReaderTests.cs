using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.Consolidation.Domain;
using CashFlow.Consolidation.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CashFlow.Consolidation.IntegrationTests;

public sealed class CachedDailyBalanceReaderTests
{
    private static readonly DateOnly Day = new(2026, 9, 29);

    [Fact]
    public async Task RepeatedReads_WithinTtl_ShouldHitStorageOnlyOnce()
    {
        var inner = new CountingReader();
        var sut = CreateSut(inner, TimeSpan.FromMinutes(1));

        for (var i = 0; i < 50; i++)
        {
            await sut.GetAsync(Day);
        }

        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task DifferentKeys_ShouldBeCachedIndependently()
    {
        var inner = new CountingReader();
        var sut = CreateSut(inner, TimeSpan.FromMinutes(1));

        await sut.GetAsync(Day);
        await sut.GetAsync(Day.AddDays(1));
        await sut.ListAsync(Day, Day.AddDays(1));
        await sut.GetAccumulatedBalanceBeforeAsync(Day);

        Assert.Equal(4, inner.Calls);
    }

    [Fact]
    public async Task AfterTtl_ShouldReloadFromStorage()
    {
        var inner = new CountingReader();
        var sut = CreateSut(inner, TimeSpan.FromMilliseconds(50));

        await sut.GetAsync(Day);
        await Task.Delay(150);
        await sut.GetAsync(Day);

        Assert.Equal(2, inner.Calls);
    }

    private static CachedDailyBalanceReader CreateSut(IDailyBalanceReader inner, TimeSpan ttl) =>
        new(inner, new MemoryCache(new MemoryCacheOptions()), Options.Create(new BalanceCacheOptions { TimeToLive = ttl }));

    private sealed class CountingReader : IDailyBalanceReader
    {
        public int Calls { get; private set; }

        public Task<DailyBalance?> GetAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<DailyBalance?>(DailyBalance.Empty(date));
        }

        public Task<IReadOnlyList<DailyBalance>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<DailyBalance>>([]);
        }

        public Task<decimal> GetAccumulatedBalanceBeforeAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(0m);
        }
    }
}
