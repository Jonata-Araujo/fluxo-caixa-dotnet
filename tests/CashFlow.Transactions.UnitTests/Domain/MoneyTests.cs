using CashFlow.Transactions.Domain;

namespace CashFlow.Transactions.UnitTests.Domain;

public sealed class MoneyTests
{
    [Theory]
    [InlineData("0.01")]
    [InlineData("10")]
    [InlineData("1234.56")]
    [InlineData("999999999.99")]
    public void Create_WithValidValue_ShouldSucceed(string raw)
    {
        var value = decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);

        var result = Money.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-100")]
    public void Create_WithZeroOrNegative_ShouldFail(string raw)
    {
        var result = Money.Create(decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture));

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionErrors.AmountMustBePositive, result.Error);
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimalPlaces_ShouldFail()
    {
        var result = Money.Create(10.555m);

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionErrors.AmountInvalidPrecision, result.Error);
    }

    [Fact]
    public void Create_AboveMaximum_ShouldFail()
    {
        var result = Money.Create(Money.MaxValue + 0.01m);

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionErrors.AmountTooLarge, result.Error);
    }

    [Fact]
    public void Money_ShouldHaveValueEquality()
    {
        Assert.Equal(Money.Create(42.10m).Value, Money.Create(42.1m).Value);
    }
}
