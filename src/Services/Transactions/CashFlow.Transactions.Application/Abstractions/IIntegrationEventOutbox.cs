namespace CashFlow.Transactions.Application.Abstractions;

/// <summary>
/// Transactional Outbox: registra o evento de integração NA MESMA transação do lançamento.
/// A publicação no broker acontece depois, de forma assíncrona. Assim, o registro de
/// lançamentos nunca depende da disponibilidade do RabbitMQ ou do serviço de Consolidado.
/// </summary>
public interface IIntegrationEventOutbox
{
    void Enqueue<TEvent>(TEvent integrationEvent, string eventName) where TEvent : class;
}
