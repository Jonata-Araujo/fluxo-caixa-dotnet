using CashFlow.Consolidation.Domain;

namespace CashFlow.Consolidation.Application.Abstractions;

/// <summary>Lado de escrita do read model de saldos (projeção alimentada por eventos).</summary>
public interface IDailyBalanceProjection
{
    /// <summary>
    /// Aplica o movimento de forma atômica e idempotente.
    /// Retorna <c>false</c> se o lançamento já havia sido aplicado (mensagem duplicada).
    /// </summary>
    Task<bool> TryApplyAsync(BalanceMovement movement, CancellationToken cancellationToken = default);
}
