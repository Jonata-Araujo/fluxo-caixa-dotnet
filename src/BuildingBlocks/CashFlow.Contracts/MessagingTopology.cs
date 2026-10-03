namespace CashFlow.Contracts;

/// <summary>Nomes da topologia RabbitMQ compartilhados entre produtor e consumidor.</summary>
public static class MessagingTopology
{
    public const string TransactionsExchange = "cashflow.transactions";
    public const string TransactionsDeadLetterExchange = "cashflow.transactions.dlx";
    public const string TransactionRegisteredRoutingKey = TransactionRegisteredIntegrationEvent.EventName;

    public const string ConsolidationQueue = "consolidation.transaction-registered";
    public const string ConsolidationDeadLetterQueue = "consolidation.transaction-registered.dlq";
}
