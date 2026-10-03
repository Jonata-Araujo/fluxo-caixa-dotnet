using CashFlow.SharedKernel;
using CashFlow.Transactions.Domain;

namespace CashFlow.Transactions.UnitTests.Domain;

public sealed class TransactionTests
{
    private static readonly DateOnly Today = new(2026, 9, 29);
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_Credit_ShouldCreateTransactionWithPositiveSignedAmount()
    {
        var result = Transaction.Register(TransactionType.Credit, 150.75m, "Venda balcão", Today, Today, Now);

        Assert.True(result.IsSuccess);
        var transaction = result.Value;
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(TransactionType.Credit, transaction.Type);
        Assert.Equal(150.75m, transaction.Amount.Value);
        Assert.Equal(150.75m, transaction.SignedAmount);
        Assert.Equal(Today, transaction.ReferenceDate);
        Assert.Equal(Now, transaction.CreatedAt);
    }

    [Fact]
    public void Register_Debit_ShouldHaveNegativeSignedAmount()
    {
        var result = Transaction.Register(TransactionType.Debit, 80m, "Pagamento fornecedor", Today, Today, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(-80m, result.Value.SignedAmount);
    }

    [Fact]
    public void Register_ShouldTrimDescription()
    {
        var result = Transaction.Register(TransactionType.Credit, 10m, "   Venda   ", Today, Today, Now);

        Assert.Equal("Venda", result.Value.Description);
    }

    [Theory]
    [InlineData((string?)null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Register_WithoutDescription_ShouldFail(string? description)
    {
        var result = Transaction.Register(TransactionType.Credit, 10m, description, Today, Today, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionErrors.DescriptionRequired, result.Error);
    }

    [Fact]
    public void Register_WithTooLongDescription_ShouldFail()
    {
        var description = new string('x', TransactionErrors.DescriptionMaxLength + 1);

        var result = Transaction.Register(TransactionType.Credit, 10m, description, Today, Today, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionErrors.DescriptionTooLong, result.Error);
    }

    [Fact]
    public void Register_WithFutureDate_ShouldFail()
    {
        var result = Transaction.Register(TransactionType.Credit, 10m, "Venda", Today.AddDays(1), Today, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionErrors.FutureDate, result.Error);
    }

    [Fact]
    public void Register_WithPastDate_ShouldSucceed()
    {
        // Lançamento retroativo (ex.: esquecido no dia anterior) é permitido.
        var result = Transaction.Register(TransactionType.Debit, 10m, "Ajuste", Today.AddDays(-3), Today, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(Today.AddDays(-3), result.Value.ReferenceDate);
    }

    [Fact]
    public void Register_WithInvalidAmount_ShouldPropagateMoneyError()
    {
        var result = Transaction.Register(TransactionType.Credit, -5m, "Venda", Today, Today, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionErrors.AmountMustBePositive, result.Error);
    }

    [Fact]
    public void Register_ShouldGenerateUniqueIds()
    {
        var first = Transaction.Register(TransactionType.Credit, 1m, "A", Today, Today, Now).Value;
        var second = Transaction.Register(TransactionType.Credit, 1m, "A", Today, Today, Now).Value;

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first, second);
    }
}
