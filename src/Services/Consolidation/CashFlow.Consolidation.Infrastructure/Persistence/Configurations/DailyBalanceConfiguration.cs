using CashFlow.Consolidation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Consolidation.Infrastructure.Persistence.Configurations;

internal sealed class DailyBalanceConfiguration : IEntityTypeConfiguration<DailyBalance>
{
    public void Configure(EntityTypeBuilder<DailyBalance> builder)
    {
        builder.ToTable("daily_balances");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("balance_date").ValueGeneratedNever();
        builder.Property(b => b.TotalCredits).HasColumnName("total_credits").HasPrecision(18, 2);
        builder.Property(b => b.TotalDebits).HasColumnName("total_debits").HasPrecision(18, 2);
        builder.Property(b => b.TransactionCount).HasColumnName("transaction_count");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(b => b.Date);
        builder.Ignore(b => b.NetBalance);
    }
}

internal sealed class ProcessedTransactionConfiguration : IEntityTypeConfiguration<ProcessedTransaction>
{
    public void Configure(EntityTypeBuilder<ProcessedTransaction> builder)
    {
        builder.ToTable("processed_transactions");

        builder.HasKey(p => p.TransactionId);
        builder.Property(p => p.TransactionId).HasColumnName("transaction_id").ValueGeneratedNever();
        builder.Property(p => p.ProcessedAt).HasColumnName("processed_at");
    }
}
