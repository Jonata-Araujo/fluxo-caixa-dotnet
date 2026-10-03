namespace CashFlow.Transactions.Infrastructure.Outbox;

/// <summary>Mensagem pendente de publicação no broker (Transactional Outbox).</summary>
public sealed class OutboxMessage
{
    private OutboxMessage()
    {
        EventName = null!;
        Payload = null!;
    }

    public OutboxMessage(Guid id, string eventName, string payload, DateTimeOffset occurredAt)
    {
        Id = id;
        EventName = eventName;
        Payload = payload;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public string EventName { get; private set; }
    public string Payload { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    public void MarkAsProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        Attempts++;
        LastError = null;
    }

    public void MarkAsFailed(string error)
    {
        Attempts++;
        LastError = error.Length > 2000 ? error[..2000] : error;
    }
}
