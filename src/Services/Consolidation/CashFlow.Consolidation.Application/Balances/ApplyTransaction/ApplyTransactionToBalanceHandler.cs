using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.Consolidation.Domain;
using CashFlow.Contracts;
using CashFlow.SharedKernel;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Application.Balances.ApplyTransaction;

public enum ProcessingOutcome
{
    Applied,
    Duplicate
}

/// <summary>
/// Caso de uso: refletir um lançamento registrado no saldo consolidado do dia.
/// Idempotente: o mesmo evento entregue mais de uma vez (at-least-once) é aplicado só uma vez.
/// </summary>
public sealed class ApplyTransactionToBalanceHandler(
    IDailyBalanceProjection projection,
    ILogger<ApplyTransactionToBalanceHandler> logger)
    : ICommandHandler<TransactionRegisteredIntegrationEvent, ProcessingOutcome>
{
    public async Task<Result<ProcessingOutcome>> HandleAsync(
        TransactionRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var type = TransactionTypeParser.Parse(integrationEvent.Type);
        if (type.IsFailure)
        {
            return type.Error;
        }

        var movement = BalanceMovement.Create(
            integrationEvent.TransactionId,
            integrationEvent.ReferenceDate,
            type.Value,
            integrationEvent.Amount);

        if (movement.IsFailure)
        {
            return movement.Error;
        }

        var applied = await projection.TryApplyAsync(movement.Value, cancellationToken);
        if (!applied)
        {
            logger.LogInformation("Lançamento {TransactionId} já consolidado; mensagem duplicada ignorada.",
                integrationEvent.TransactionId);
            return ProcessingOutcome.Duplicate;
        }

        return ProcessingOutcome.Applied;
    }
}
