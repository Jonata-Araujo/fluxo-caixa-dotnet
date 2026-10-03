using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.Domain;

/// <summary>
/// Saldo consolidado de um dia (read model materializado).
/// Saldo do dia = total de créditos − total de débitos.
/// </summary>
public sealed class DailyBalance : Entity<DateOnly>
{
    private DailyBalance() : base(default)
    {
    }

    private DailyBalance(DateOnly date) : base(date)
    {
    }

    public DateOnly Date => Id;
    public decimal TotalCredits { get; private set; }
    public decimal TotalDebits { get; private set; }
    public int TransactionCount { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public decimal NetBalance => TotalCredits - TotalDebits;

    public static DailyBalance Empty(DateOnly date) => new(date);

    public static DailyBalance Restore(DateOnly date, decimal totalCredits, decimal totalDebits, int transactionCount, DateTimeOffset updatedAt) =>
        new(date)
        {
            TotalCredits = totalCredits,
            TotalDebits = totalDebits,
            TransactionCount = transactionCount,
            UpdatedAt = updatedAt
        };

    /// <summary>
    /// Aplica um movimento ao saldo do dia. Na persistência, a mesma regra é executada
    /// de forma atômica no banco (upsert incremental) para suportar concorrência.
    /// </summary>
    public void Apply(BalanceMovement movement, DateTimeOffset appliedAt)
    {
        if (movement.Date != Date)
        {
            throw new InvalidOperationException(
                $"Movimento de {movement.Date:yyyy-MM-dd} não pode ser aplicado ao saldo de {Date:yyyy-MM-dd}.");
        }

        TotalCredits += movement.CreditAmount;
        TotalDebits += movement.DebitAmount;
        TransactionCount++;
        UpdatedAt = appliedAt;
    }
}
