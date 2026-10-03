using CashFlow.Consolidation.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Persistence;

public sealed class ConsolidationDbContext(DbContextOptions<ConsolidationDbContext> options) : DbContext(options)
{
    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();
    public DbSet<ProcessedTransaction> ProcessedTransactions => Set<ProcessedTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConsolidationDbContext).Assembly);
}

/// <summary>Registro de idempotência: lançamentos já refletidos no saldo.</summary>
public sealed class ProcessedTransaction
{
    public Guid TransactionId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
