using CashFlow.SharedKernel;

namespace CashFlow.Transactions.UnitTests.Domain;

public sealed class TransactionTypeParserTests
{
    [Theory]
    [InlineData("credit", TransactionType.Credit)]
    [InlineData("CREDIT", TransactionType.Credit)]
    [InlineData(" crédito ", TransactionType.Credit)]
    [InlineData("debit", TransactionType.Debit)]
    [InlineData("Debito", TransactionType.Debit)]
    public void Parse_WithKnownValues_ShouldSucceed(string input, TransactionType expected)
    {
        var result = TransactionTypeParser.Parse(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null, "transaction.type.required")]
    [InlineData("", "transaction.type.required")]
    [InlineData("transfer", "transaction.type.invalid")]
    public void Parse_WithInvalidValues_ShouldFail(string? input, string expectedCode)
    {
        var result = TransactionTypeParser.Parse(input);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }
}
