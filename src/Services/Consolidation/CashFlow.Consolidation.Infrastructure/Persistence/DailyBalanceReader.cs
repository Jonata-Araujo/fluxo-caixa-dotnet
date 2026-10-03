using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.Consolidation.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Persistence;

internal sealed class DailyBalanceReader(ConsolidationDbContext dbContext) : IDailyBalanceReader
{
    public Task<DailyBalance?> GetAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        dbContext.DailyBalances.AsNoTracking().FirstOrDefaultAsync(b => b.Id == date, cancellationToken);

    public async Task<IReadOnlyList<DailyBalance>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await dbContext.DailyBalances
            .AsNoTracking()
            .Where(b => b.Id >= from && b.Id <= to)
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetAccumulatedBalanceBeforeAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        await dbContext.DailyBalances
            .Where(b => b.Id < date)
            .SumAsync(b => (decimal?)(b.TotalCredits - b.TotalDebits), cancellationToken) ?? 0m;
}
