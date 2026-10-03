using CashFlow.SharedKernel;

namespace CashFlow.Transactions.Domain;

/// <summary>
/// Agregado raiz: um lançamento financeiro (crédito ou débito) no caixa do lojista.
/// Lançamentos são imutáveis após registrados – correções devem ser feitas por estorno
/// (novo lançamento de natureza oposta), preservando a trilha de auditoria.
/// </summary>
public sealed class Transaction : Entity<Guid>
{
    private Transaction(
        Guid id,
        TransactionType type,
        Money amount,
        string description,
        DateOnly referenceDate,
        DateTimeOffset createdAt) : base(id)
    {
        Type = type;
        Amount = amount;
        Description = description;
        ReferenceDate = referenceDate;
        CreatedAt = createdAt;
    }

    /// <summary>Construtor exclusivo para materialização pelo ORM.</summary>
    private Transaction() : base(Guid.Empty)
    {
        Amount = null!;
        Description = null!;
    }

    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public string Description { get; private set; }

    /// <summary>Data de competência: o dia do caixa ao qual o lançamento pertence.</summary>
    public DateOnly ReferenceDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Valor com sinal: positivo para crédito, negativo para débito.</summary>
    public decimal SignedAmount => Type == TransactionType.Credit ? Amount.Value : -Amount.Value;

    /// <summary>
    /// Factory method que garante que um <see cref="Transaction"/> nunca exista em estado inválido.
    /// </summary>
    /// <param name="today">Data corrente no fuso do lojista (injetada para manter o domínio determinístico).</param>
    public static Result<Transaction> Register(
        TransactionType type,
        decimal amount,
        string? description,
        DateOnly referenceDate,
        DateOnly today,
        DateTimeOffset now)
    {
        var money = Money.Create(amount);
        if (money.IsFailure)
        {
            return money.Error;
        }

        var normalizedDescription = description?.Trim();
        if (string.IsNullOrEmpty(normalizedDescription))
        {
            return TransactionErrors.DescriptionRequired;
        }

        if (normalizedDescription.Length > TransactionErrors.DescriptionMaxLength)
        {
            return TransactionErrors.DescriptionTooLong;
        }

        if (referenceDate > today)
        {
            return TransactionErrors.FutureDate;
        }

        return new Transaction(Guid.NewGuid(), type, money.Value, normalizedDescription, referenceDate, now);
    }

    /// <summary>Reidrata o agregado a partir da persistência (dados já validados).</summary>
    public static Transaction Restore(
        Guid id,
        TransactionType type,
        decimal amount,
        string description,
        DateOnly referenceDate,
        DateTimeOffset createdAt) =>
        new(id, type, Money.Restore(amount), description, referenceDate, createdAt);
}
