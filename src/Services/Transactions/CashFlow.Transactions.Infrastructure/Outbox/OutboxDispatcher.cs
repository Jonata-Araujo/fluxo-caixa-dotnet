using CashFlow.Transactions.Infrastructure.Messaging;
using CashFlow.Transactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashFlow.Transactions.Infrastructure.Outbox;

/// <summary>
/// Lê um lote de mensagens pendentes e publica no broker (entrega at-least-once).
/// <c>FOR UPDATE SKIP LOCKED</c> permite várias instâncias do serviço processando o outbox
/// em paralelo sem publicar a mesma mensagem duas vezes no mesmo ciclo.
/// </summary>
internal sealed class OutboxDispatcher(
    TransactionsDbContext dbContext,
    IIntegrationEventPublisher publisher,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcher> logger)
{
    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var messages = await dbContext.OutboxMessages
            .FromSqlRaw(
                """
                SELECT * FROM outbox_messages
                WHERE processed_at IS NULL
                ORDER BY occurred_at
                LIMIT {0}
                FOR UPDATE SKIP LOCKED
                """,
                options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        var published = 0;
        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(message, cancellationToken);
                message.MarkAsProcessed(timeProvider.GetUtcNow());
                published++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Broker indisponível: registra a falha e interrompe o lote (preserva a ordem e
                // evita martelar um broker fora do ar). O lançamento JÁ está salvo; o evento
                // será publicado no próximo ciclo – consistência eventual.
                message.MarkAsFailed(ex.Message);
                logger.LogWarning(ex, "Falha ao publicar mensagem {MessageId} do outbox (tentativa {Attempts}).",
                    message.Id, message.Attempts);
                break;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        return published;
    }
}
