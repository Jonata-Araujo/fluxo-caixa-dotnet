using CashFlow.Transactions.Infrastructure.Outbox;

namespace CashFlow.Transactions.Infrastructure.Messaging;

internal interface IIntegrationEventPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}
