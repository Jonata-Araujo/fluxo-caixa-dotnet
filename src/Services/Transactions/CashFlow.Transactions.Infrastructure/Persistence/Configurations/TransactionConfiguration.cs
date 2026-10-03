using CashFlow.SharedKernel;
using CashFlow.Transactions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Transactions.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasConversion(money => money.Value, value => Money.Restore(value))
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(TransactionErrors.DescriptionMaxLength)
            .IsRequired();

        builder.Property(t => t.ReferenceDate).HasColumnName("reference_date").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Ignore(t => t.SignedAmount);

        // Consulta principal: lançamentos de um dia, em ordem de registro.
        builder.HasIndex(t => new { t.ReferenceDate, t.CreatedAt })
            .HasDatabaseName("ix_transactions_reference_date_created_at");
    }
}
