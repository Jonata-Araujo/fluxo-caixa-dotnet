using System.Text.Json;
using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Infrastructure.Persistence;

namespace CashFlow.Transactions.Infrastructure.Outbox;

internal sealed class EfIntegrationEventOutbox(TransactionsDbContext dbContext, TimeProvider timeProvider)
    : IIntegrationEventOutbox
{
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public void Enqueue<TEvent>(TEvent integrationEvent, string eventName) where TEvent : class
    {
        var payload = JsonSerializer.Serialize(integrationEvent, SerializerOptions);
        dbContext.OutboxMessages.Add(new OutboxMessage(Guid.NewGuid(), eventName, payload, timeProvider.GetUtcNow()));
    }
}
