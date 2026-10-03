using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Domain;

namespace CashFlow.Transactions.UnitTests.Fakes;

/*
 * Test doubles escritos à mão (sem biblioteca de mock): deixam os testes focados em
 * comportamento observável e não em detalhes de chamadas internas.
 */

internal sealed class FixedBusinessClock(DateOnly today) : IBusinessClock
{
    public DateTimeOffset Now { get; } = new(today.ToDateTime(new TimeOnly(15, 0)), TimeSpan.Zero);
    public DateOnly Today { get; } = today;
}

internal sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> _pending = [];

    public List<Transaction> Committed { get; } = [];

    public void Add(Transaction transaction) => _pending.Add(transaction);

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Committed.FirstOrDefault(t => t.Id == id));

    public Task<(IReadOnlyList<Transaction> Items, long Total)> ListByDateAsync(
        DateOnly date, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = Committed.Where(t => t.ReferenceDate == date).OrderBy(t => t.CreatedAt).ToList();
        IReadOnlyList<Transaction> items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((items, (long)all.Count));
    }

    internal void Commit()
    {
        Committed.AddRange(_pending);
        _pending.Clear();
    }
}

internal sealed class InMemoryOutbox : IIntegrationEventOutbox
{
    private readonly List<(object Event, string Name)> _pending = [];

    public List<(object Event, string Name)> Committed { get; } = [];

    public void Enqueue<TEvent>(TEvent integrationEvent, string eventName) where TEvent : class =>
        _pending.Add((integrationEvent, eventName));

    internal void Commit()
    {
        Committed.AddRange(_pending);
        _pending.Clear();
    }
}

/// <summary>Simula a transação: só "efetiva" repositório e outbox juntos no SaveChanges.</summary>
internal sealed class FakeUnitOfWork(InMemoryTransactionRepository repository, InMemoryOutbox outbox) : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        repository.Commit();
        outbox.Commit();
        return Task.FromResult(1);
    }
}
