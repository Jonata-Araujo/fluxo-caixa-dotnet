using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.Consolidation.Domain;

namespace CashFlow.Consolidation.UnitTests.Fakes;

/// <summary>
/// Implementação em memória com a mesma semântica da projeção real (idempotente por TransactionId).
/// Implementa leitura e escrita para permitir testes de ponta a ponta da camada de aplicação.
/// </summary>
internal sealed class InMemoryDailyBalanceStore : IDailyBalanceProjection, IDailyBalanceReader
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    private readonly Dictionary<DateOnly, DailyBalance> _balances = [];
    private readonly HashSet<Guid> _processed = [];

    public int ReadCount { get; private set; }

    public Task<bool> TryApplyAsync(BalanceMovement movement, CancellationToken cancellationToken = default)
    {
        if (!_processed.Add(movement.TransactionId))
        {
            return Task.FromResult(false);
        }

        if (!_balances.TryGetValue(movement.Date, out var balance))
        {
            balance = DailyBalance.Empty(movement.Date);
            _balances[movement.Date] = balance;
        }

        balance.Apply(movement, Now);
        return Task.FromResult(true);
    }

    public Task<DailyBalance?> GetAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult(_balances.GetValueOrDefault(date));
    }

    public Task<IReadOnlyList<DailyBalance>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        IReadOnlyList<DailyBalance> list = _balances.Values.Where(b => b.Date >= from && b.Date <= to).OrderBy(b => b.Date).ToList();
        return Task.FromResult(list);
    }

    public Task<decimal> GetAccumulatedBalanceBeforeAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult(_balances.Values.Where(b => b.Date < date).Sum(b => b.NetBalance));
    }
}
