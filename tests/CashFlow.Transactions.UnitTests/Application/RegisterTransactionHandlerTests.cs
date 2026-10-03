using CashFlow.Contracts;
using CashFlow.Transactions.Application.Transactions.Register;
using CashFlow.Transactions.Domain;
using CashFlow.Transactions.UnitTests.Fakes;

namespace CashFlow.Transactions.UnitTests.Application;

public sealed class RegisterTransactionHandlerTests
{
    private static readonly DateOnly Today = new(2026, 9, 29);

    private readonly InMemoryTransactionRepository _repository = new();
    private readonly InMemoryOutbox _outbox = new();
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly RegisterTransactionHandler _handler;

    public RegisterTransactionHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork(_repository, _outbox);
        _handler = new RegisterTransactionHandler(_repository, _outbox, _unitOfWork, new FixedBusinessClock(Today));
    }

    [Fact]
    public async Task Handle_ValidCredit_ShouldPersistTransactionAndReturnResponse()
    {
        var result = await _handler.HandleAsync(new RegisterTransactionCommand("credit", 250.40m, "Venda cartão", Today));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_repository.Committed);
        Assert.Equal(stored.Id, result.Value.Id);
        Assert.Equal("Credit", result.Value.Type);
        Assert.Equal(250.40m, result.Value.SignedAmount);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_ValidTransaction_ShouldEnqueueIntegrationEventInSameUnitOfWork()
    {
        var result = await _handler.HandleAsync(new RegisterTransactionCommand("debit", 99.90m, "Conta de luz", Today));

        Assert.True(result.IsSuccess);
        var (message, name) = Assert.Single(_outbox.Committed);
        Assert.Equal(TransactionRegisteredIntegrationEvent.EventName, name);

        var integrationEvent = Assert.IsType<TransactionRegisteredIntegrationEvent>(message);
        Assert.Equal(result.Value.Id, integrationEvent.TransactionId);
        Assert.Equal("Debit", integrationEvent.Type);
        Assert.Equal(99.90m, integrationEvent.Amount);
        Assert.Equal(Today, integrationEvent.ReferenceDate);
        Assert.NotEqual(Guid.Empty, integrationEvent.EventId);
    }

    [Fact]
    public async Task Handle_WithoutDate_ShouldUseMerchantToday()
    {
        var result = await _handler.HandleAsync(new RegisterTransactionCommand("credit", 10m, "Venda", Date: null));

        Assert.True(result.IsSuccess);
        Assert.Equal(Today, result.Value.Date);
    }

    [Fact]
    public async Task Handle_InvalidType_ShouldFailWithoutPersistingAnything()
    {
        var result = await _handler.HandleAsync(new RegisterTransactionCommand("pix", 10m, "Venda", Today));

        Assert.True(result.IsFailure);
        Assert.Equal("transaction.type.invalid", result.Error.Code);
        AssertNothingPersisted();
    }

    [Fact]
    public async Task Handle_InvalidDomainRule_ShouldFailWithoutPersistingAnything()
    {
        var result = await _handler.HandleAsync(new RegisterTransactionCommand("credit", 0m, "Venda", Today));

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionErrors.AmountMustBePositive, result.Error);
        AssertNothingPersisted();
    }

    [Fact]
    public async Task Handle_FutureDate_ShouldFail()
    {
        var result = await _handler.HandleAsync(new RegisterTransactionCommand("credit", 10m, "Venda", Today.AddDays(1)));

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionErrors.FutureDate, result.Error);
        AssertNothingPersisted();
    }

    private void AssertNothingPersisted()
    {
        Assert.Empty(_repository.Committed);
        Assert.Empty(_outbox.Committed);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
