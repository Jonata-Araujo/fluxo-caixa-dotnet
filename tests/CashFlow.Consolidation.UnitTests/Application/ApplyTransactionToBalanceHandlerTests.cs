using CashFlow.Consolidation.Application.Balances.ApplyTransaction;
using CashFlow.Consolidation.Domain;
using CashFlow.Consolidation.UnitTests.Fakes;
using CashFlow.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace CashFlow.Consolidation.UnitTests.Application;

public sealed class ApplyTransactionToBalanceHandlerTests
{
    private static readonly DateOnly Day = new(2026, 9, 29);
    private readonly InMemoryDailyBalanceStore _store = new();
    private readonly ApplyTransactionToBalanceHandler _handler;

    public ApplyTransactionToBalanceHandlerTests() =>
        _handler = new ApplyTransactionToBalanceHandler(_store, NullLogger<ApplyTransactionToBalanceHandler>.Instance);

    [Fact]
    public async Task Handle_NewEvent_ShouldApplyToDailyBalance()
    {
        var result = await _handler.HandleAsync(Event("Credit", 120m));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProcessingOutcome.Applied, result.Value);
        var balance = await _store.GetAsync(Day);
        Assert.Equal(120m, balance!.NetBalance);
    }

    [Fact]
    public async Task Handle_SameEventTwice_ShouldBeIdempotent()
    {
        var integrationEvent = Event("Debit", 40m);

        var first = await _handler.HandleAsync(integrationEvent);
        var second = await _handler.HandleAsync(integrationEvent);

        Assert.Equal(ProcessingOutcome.Applied, first.Value);
        Assert.Equal(ProcessingOutcome.Duplicate, second.Value);
        var balance = await _store.GetAsync(Day);
        Assert.Equal(-40m, balance!.NetBalance);
        Assert.Equal(1, balance.TransactionCount);
    }

    [Fact]
    public async Task Handle_InvalidType_ShouldReturnValidationError()
    {
        var result = await _handler.HandleAsync(Event("Transfer", 10m));

        Assert.True(result.IsFailure);
        Assert.Equal("transaction.type.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_InvalidAmount_ShouldReturnValidationError()
    {
        var result = await _handler.HandleAsync(Event("Credit", -10m));

        Assert.Equal(ConsolidationErrors.AmountMustBePositive, result.Error);
        Assert.Null(await _store.GetAsync(Day));
    }

    private static TransactionRegisteredIntegrationEvent Event(string type, decimal amount) =>
        new(Guid.NewGuid(), Guid.NewGuid(), type, amount, Day, DateTimeOffset.UtcNow);
}
