using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transactions.Infrastructure.Persistence;

internal sealed class TransactionRepository(TransactionsDbContext dbContext) : ITransactionRepository
{
    public void Add(Transaction transaction) => dbContext.Transactions.Add(transaction);

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Transaction> Items, long Total)> ListByDateAsync(
        DateOnly date, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Transactions.AsNoTracking().Where(t => t.ReferenceDate == date);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
