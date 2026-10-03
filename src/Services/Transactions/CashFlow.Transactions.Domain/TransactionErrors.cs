using CashFlow.SharedKernel;

namespace CashFlow.Transactions.Domain;

/// <summary>Catálogo de erros do domínio de lançamentos (códigos estáveis para o cliente).</summary>
public static class TransactionErrors
{
    public const int DescriptionMaxLength = 200;

    public static readonly Error AmountMustBePositive =
        Error.Validation("transaction.amount.non_positive", "O valor do lançamento deve ser maior que zero.");

    public static readonly Error AmountTooLarge =
        Error.Validation("transaction.amount.too_large", $"O valor do lançamento deve ser no máximo {Money.MaxValue:N2}.");

    public static readonly Error AmountInvalidPrecision =
        Error.Validation("transaction.amount.precision", "O valor do lançamento deve ter no máximo 2 casas decimais.");

    public static readonly Error DescriptionRequired =
        Error.Validation("transaction.description.required", "A descrição do lançamento é obrigatória.");

    public static readonly Error DescriptionTooLong =
        Error.Validation("transaction.description.too_long", $"A descrição deve ter no máximo {DescriptionMaxLength} caracteres.");

    public static readonly Error FutureDate =
        Error.Validation("transaction.date.future", "Não é permitido registrar lançamentos com data futura.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("transaction.not_found", $"Lançamento '{id}' não encontrado.");
}
