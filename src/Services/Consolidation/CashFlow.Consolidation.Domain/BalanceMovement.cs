using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.Domain;

/// <summary>
/// Value Object: o efeito de um lançamento sobre o saldo de um dia.
/// É a tradução, para a linguagem do contexto de Consolidação, do evento recebido
/// do contexto de Lançamentos (Anti-Corruption Layer simplificada).
/// </summary>
public sealed record BalanceMovement
{
    private BalanceMovement(Guid transactionId, DateOnly date, TransactionType type, decimal amount)
    {
        TransactionId = transactionId;
        Date = date;
        Type = type;
        Amount = amount;
    }

    public Guid TransactionId { get; }
    public DateOnly Date { get; }
    public TransactionType Type { get; }
    public decimal Amount { get; }

    public decimal CreditAmount => Type == TransactionType.Credit ? Amount : 0m;
    public decimal DebitAmount => Type == TransactionType.Debit ? Amount : 0m;

    public static Result<BalanceMovement> Create(Guid transactionId, DateOnly date, TransactionType type, decimal amount)
    {
        if (transactionId == Guid.Empty)
        {
            return ConsolidationErrors.InvalidTransactionId;
        }

        if (amount <= 0)
        {
            return ConsolidationErrors.AmountMustBePositive;
        }

        return new BalanceMovement(transactionId, date, type, amount);
    }
}
