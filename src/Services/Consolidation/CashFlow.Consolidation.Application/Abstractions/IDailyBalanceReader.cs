using CashFlow.Consolidation.Domain;

namespace CashFlow.Consolidation.Application.Abstractions;

/// <summary>Lado de leitura dos saldos consolidados (otimizado e cacheável).</summary>
public interface IDailyBalanceReader
{
    Task<DailyBalance?> GetAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyBalance>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Soma dos saldos de todos os dias anteriores a <paramref name="date"/>.</summary>
    Task<decimal> GetAccumulatedBalanceBeforeAsync(DateOnly date, CancellationToken cancellationToken = default);
}
