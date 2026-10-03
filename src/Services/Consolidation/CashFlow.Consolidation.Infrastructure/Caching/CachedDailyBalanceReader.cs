using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.Consolidation.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CashFlow.Consolidation.Infrastructure.Caching;

public sealed class BalanceCacheOptions
{
    public const string SectionName = "BalanceCache";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Tempo de vida das entradas. O saldo já é eventualmente consistente (alimentado por fila);
    /// alguns segundos a mais de defasagem reduzem drasticamente a carga no banco em picos.
    /// </summary>
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Decorator (padrão GoF) sobre <see cref="IDailyBalanceReader"/>: adiciona cache em memória
/// sem alterar a implementação de acesso a dados nem os casos de uso.
/// Em múltiplas instâncias, basta trocar por IDistributedCache (Redis) – ver README.
/// </summary>
internal sealed class CachedDailyBalanceReader(
    IDailyBalanceReader inner,
    IMemoryCache cache,
    IOptions<BalanceCacheOptions> options) : IDailyBalanceReader
{
    private readonly TimeSpan _ttl = options.Value.TimeToLive;

    public Task<DailyBalance?> GetAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        GetOrCreateAsync($"balance:{date:yyyy-MM-dd}", () => inner.GetAsync(date, cancellationToken));

    public Task<IReadOnlyList<DailyBalance>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        GetOrCreateAsync($"balances:{from:yyyy-MM-dd}:{to:yyyy-MM-dd}", () => inner.ListAsync(from, to, cancellationToken));

    public Task<decimal> GetAccumulatedBalanceBeforeAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        GetOrCreateAsync($"accumulated:{date:yyyy-MM-dd}", () => inner.GetAccumulatedBalanceBeforeAsync(date, cancellationToken));

    private async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
    {
        if (cache.TryGetValue(key, out T? cached))
        {
            return cached!;
        }

        var value = await factory();
        cache.Set(key, value, _ttl);
        return value;
    }
}
