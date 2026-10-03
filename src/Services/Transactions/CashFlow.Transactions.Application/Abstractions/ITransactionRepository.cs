using CashFlow.Transactions.Domain;

namespace CashFlow.Transactions.Application.Abstractions;

public interface ITransactionRepository
{
    void Add(Transaction transaction);

    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Transaction> Items, long Total)> ListByDateAsync(
        DateOnly date, int page, int pageSize, CancellationToken cancellationToken = default);
}
