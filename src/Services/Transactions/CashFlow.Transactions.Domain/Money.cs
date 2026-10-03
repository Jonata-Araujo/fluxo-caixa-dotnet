using CashFlow.SharedKernel;

namespace CashFlow.Transactions.Domain;

/// <summary>
/// Value Object que representa um valor monetário de lançamento.
/// Invariantes: estritamente positivo e com no máximo 2 casas decimais.
/// O sinal (entrada/saída) é responsabilidade do <see cref="TransactionType"/>, não do valor.
/// </summary>
public sealed record Money
{
    public const decimal MaxValue = 999_999_999.99m;

    private Money(decimal value) => Value = value;

    public decimal Value { get; }

    public static Result<Money> Create(decimal value)
    {
        if (value <= 0)
        {
            return TransactionErrors.AmountMustBePositive;
        }

        if (value > MaxValue)
        {
            return TransactionErrors.AmountTooLarge;
        }

        if (decimal.Round(value, 2) != value)
        {
            return TransactionErrors.AmountInvalidPrecision;
        }

        return new Money(value);
    }

    /// <summary>Reconstitui um valor já validado (ex.: vindo do banco). Não revalida.</summary>
    public static Money Restore(decimal value) => new(value);

    public override string ToString() => Value.ToString("N2");
}
