namespace CashFlow.SharedKernel;

/// <summary>
/// Natureza do lançamento. Faz parte da linguagem ubíqua compartilhada entre
/// os dois contextos (Lançamentos e Consolidado).
/// </summary>
public enum TransactionType
{
    Credit = 1,
    Debit = 2
}

public static class TransactionTypeParser
{
    public static Result<TransactionType> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Error.Validation("transaction.type.required", "O tipo do lançamento é obrigatório (credit ou debit).");
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "credit" or "credito" or "crédito" or "c" => TransactionType.Credit,
            "debit" or "debito" or "débito" or "d" => TransactionType.Debit,
            _ => Error.Validation("transaction.type.invalid", $"Tipo de lançamento '{value}' inválido. Use 'credit' ou 'debit'.")
        };
    }
}
