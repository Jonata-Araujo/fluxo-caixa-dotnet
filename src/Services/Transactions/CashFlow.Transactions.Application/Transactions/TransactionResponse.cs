using CashFlow.Transactions.Domain;

namespace CashFlow.Transactions.Application.Transactions;

public sealed record TransactionResponse(
    Guid Id,
    string Type,
    decimal Amount,
    decimal SignedAmount,
    string Description,
    DateOnly Date,
    DateTimeOffset CreatedAt)
{
    public static TransactionResponse From(Transaction transaction) => new(
        transaction.Id,
        transaction.Type.ToString(),
        transaction.Amount.Value,
        transaction.SignedAmount,
        transaction.Description,
        transaction.ReferenceDate,
        transaction.CreatedAt);
}
