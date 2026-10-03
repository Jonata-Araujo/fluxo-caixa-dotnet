using System.Text.Json;
using CashFlow.Consolidation.Application.Balances.ApplyTransaction;
using CashFlow.Contracts;
using CashFlow.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashFlow.Consolidation.Infrastructure.Messaging;

/// <summary>O que fazer com a mensagem após o processamento.</summary>
public enum MessageDisposition
{
    /// <summary>Processada (ou duplicada): remover da fila.</summary>
    Ack,

    /// <summary>Falha transitória (ex.: banco fora): devolver à fila para nova tentativa.</summary>
    Requeue,

    /// <summary>Mensagem inválida (poison message): enviar para a DLQ, nunca será processável.</summary>
    DeadLetter
}

/// <summary>
/// Separa a política de tratamento de mensagens (retry, poison message, idempotência) do
/// transporte RabbitMQ, o que torna essa lógica testável sem broker.
/// </summary>
internal sealed class TransactionRegisteredMessageHandler(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<TransactionRegisteredMessageHandler> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<MessageDisposition> HandleAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        TransactionRegisteredIntegrationEvent? integrationEvent;
        try
        {
            integrationEvent = JsonSerializer.Deserialize<TransactionRegisteredIntegrationEvent>(body.Span, SerializerOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Mensagem com payload inválido enviada para a DLQ.");
            return MessageDisposition.DeadLetter;
        }

        if (integrationEvent is null)
        {
            logger.LogError("Mensagem vazia enviada para a DLQ.");
            return MessageDisposition.DeadLetter;
        }

        var maxAttempts = Math.Max(1, options.Value.MaxProcessingAttempts);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider
                    .GetRequiredService<ICommandHandler<TransactionRegisteredIntegrationEvent, ProcessingOutcome>>();

                var result = await handler.HandleAsync(integrationEvent, cancellationToken);
                if (result.IsFailure)
                {
                    logger.LogError("Evento {EventId} rejeitado por regra de negócio ({Code}: {Message}). Enviado para a DLQ.",
                        integrationEvent.EventId, result.Error.Code, result.Error.Message);
                    return MessageDisposition.DeadLetter;
                }

                return MessageDisposition.Ack;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (attempt >= maxAttempts)
                {
                    logger.LogError(ex, "Falha ao processar evento {EventId} após {Attempts} tentativas. Devolvendo à fila.",
                        integrationEvent.EventId, attempt);
                    return MessageDisposition.Requeue;
                }

                var delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
                logger.LogWarning(ex, "Falha transitória ao processar evento {EventId} (tentativa {Attempt}/{Max}). Retry em {Delay}.",
                    integrationEvent.EventId, attempt, maxAttempts, delay);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
