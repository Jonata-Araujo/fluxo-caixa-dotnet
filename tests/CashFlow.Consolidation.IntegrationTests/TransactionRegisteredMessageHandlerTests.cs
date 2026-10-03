using System.Text;
using System.Text.Json;
using CashFlow.Consolidation.Application.Balances.ApplyTransaction;
using CashFlow.Consolidation.Infrastructure.Messaging;
using CashFlow.Contracts;
using CashFlow.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CashFlow.Consolidation.IntegrationTests;

/// <summary>
/// Política de tratamento de mensagens (ack / requeue / DLQ) testada sem broker.
/// </summary>
public sealed class TransactionRegisteredMessageHandlerTests
{
    [Fact]
    public async Task ValidMessage_ShouldBeAcked()
    {
        var fake = new FakeHandler(_ => ProcessingOutcome.Applied);

        var disposition = await CreateSut(fake).HandleAsync(ValidBody(), CancellationToken.None);

        Assert.Equal(MessageDisposition.Ack, disposition);
        Assert.Equal(1, fake.Calls);
    }

    [Fact]
    public async Task DuplicateMessage_ShouldBeAcked()
    {
        var disposition = await CreateSut(new FakeHandler(_ => ProcessingOutcome.Duplicate)).HandleAsync(ValidBody(), CancellationToken.None);

        Assert.Equal(MessageDisposition.Ack, disposition);
    }

    [Fact]
    public async Task MalformedJson_ShouldGoToDeadLetter()
    {
        var fake = new FakeHandler(_ => ProcessingOutcome.Applied);

        var disposition = await CreateSut(fake).HandleAsync(Encoding.UTF8.GetBytes("{not-json"), CancellationToken.None);

        Assert.Equal(MessageDisposition.DeadLetter, disposition);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task BusinessRuleViolation_ShouldGoToDeadLetterWithoutRetry()
    {
        var fake = new FakeHandler(_ => Error.Validation("x", "inválido"));

        var disposition = await CreateSut(fake).HandleAsync(ValidBody(), CancellationToken.None);

        Assert.Equal(MessageDisposition.DeadLetter, disposition);
        Assert.Equal(1, fake.Calls);
    }

    [Fact]
    public async Task TransientFailure_ShouldRetryInMemoryAndSucceed()
    {
        var fake = new FakeHandler(call => call == 1 ? throw new TimeoutException("db") : ProcessingOutcome.Applied);

        var disposition = await CreateSut(fake).HandleAsync(ValidBody(), CancellationToken.None);

        Assert.Equal(MessageDisposition.Ack, disposition);
        Assert.Equal(2, fake.Calls);
    }

    [Fact]
    public async Task PersistentFailure_ShouldRequeueAfterMaxAttempts()
    {
        var fake = new FakeHandler(_ => throw new TimeoutException("db fora"));

        var disposition = await CreateSut(fake, maxAttempts: 3).HandleAsync(ValidBody(), CancellationToken.None);

        Assert.Equal(MessageDisposition.Requeue, disposition);
        Assert.Equal(3, fake.Calls);
    }

    private static TransactionRegisteredMessageHandler CreateSut(FakeHandler fake, int maxAttempts = 3)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandler<TransactionRegisteredIntegrationEvent, ProcessingOutcome>>(fake);
        var provider = services.BuildServiceProvider();

        return new TransactionRegisteredMessageHandler(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RabbitMqOptions { MaxProcessingAttempts = maxAttempts }),
            NullLogger<TransactionRegisteredMessageHandler>.Instance);
    }

    private static byte[] ValidBody() => JsonSerializer.SerializeToUtf8Bytes(
        new TransactionRegisteredIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), "Credit", 10m, new DateOnly(2026, 9, 29), DateTimeOffset.UtcNow),
        new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private sealed class FakeHandler(Func<int, Result<ProcessingOutcome>> behavior)
        : ICommandHandler<TransactionRegisteredIntegrationEvent, ProcessingOutcome>
    {
        public int Calls { get; private set; }

        public Task<Result<ProcessingOutcome>> HandleAsync(TransactionRegisteredIntegrationEvent command, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(behavior(Calls));
        }
    }
}
