using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Domain;
using CashFlow.Transactions.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transactions.Infrastructure.Persistence;

/// <summary>
/// DbContext do serviço de Lançamentos. Também atua como Unit of Work: o lançamento e a
/// mensagem de outbox são gravados no mesmo SaveChanges (mesma transação do PostgreSQL).
/// </summary>
public sealed class TransactionsDbContext(DbContextOptions<TransactionsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);
}
