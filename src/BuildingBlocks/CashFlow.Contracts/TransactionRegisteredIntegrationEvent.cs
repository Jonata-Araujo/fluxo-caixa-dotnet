namespace CashFlow.Contracts;

/// <summary>
/// Evento de integração publicado pelo serviço de Lançamentos e consumido pelo Consolidado.
/// É o ÚNICO acoplamento entre os dois serviços: um contrato versionado e serializável.
/// </summary>
/// <param name="EventId">Identificador único do evento (usado como MessageId).</param>
/// <param name="TransactionId">Identificador do lançamento – chave de idempotência no consumidor.</param>
/// <param name="Type">"Credit" ou "Debit".</param>
/// <param name="Amount">Valor sempre positivo; o sinal é dado por <paramref name="Type"/>.</param>
/// <param name="ReferenceDate">Data de competência do lançamento (dia do caixa).</param>
/// <param name="OccurredAt">Momento em que o lançamento foi registrado.</param>
public sealed record TransactionRegisteredIntegrationEvent(
    Guid EventId,
    Guid TransactionId,
    string Type,
    decimal Amount,
    DateOnly ReferenceDate,
    DateTimeOffset OccurredAt)
{
    public const string EventName = "transaction.registered";
    public const int Version = 1;
}
