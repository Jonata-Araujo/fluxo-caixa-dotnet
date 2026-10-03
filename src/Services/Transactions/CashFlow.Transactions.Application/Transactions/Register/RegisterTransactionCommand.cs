namespace CashFlow.Transactions.Application.Transactions.Register;

/// <param name="Type">"credit" ou "debit".</param>
/// <param name="Date">Data de competência; quando omitida, assume o dia corrente do lojista.</param>
public sealed record RegisterTransactionCommand(
    string? Type,
    decimal Amount,
    string? Description,
    DateOnly? Date);
