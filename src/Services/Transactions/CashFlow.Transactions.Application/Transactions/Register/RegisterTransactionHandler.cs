using CashFlow.Contracts;
using CashFlow.SharedKernel;
using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Domain;

namespace CashFlow.Transactions.Application.Transactions.Register;

/// <summary>
/// Caso de uso: registrar um lançamento (crédito/débito).
/// Persiste o lançamento e o evento de integração atomicamente (Outbox), sem
/// nenhuma chamada síncrona ao serviço de Consolidado.
/// </summary>
public sealed class RegisterTransactionHandler(
    ITransactionRepository repository,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    IBusinessClock clock)
    : ICommandHandler<RegisterTransactionCommand, TransactionResponse>
{
    public async Task<Result<TransactionResponse>> HandleAsync(
        RegisterTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var type = TransactionTypeParser.Parse(command.Type);
        if (type.IsFailure)
        {
            return type.Error;
        }

        var today = clock.Today;
        var registration = Transaction.Register(
            type.Value,
            command.Amount,
            command.Description,
            command.Date ?? today,
            today,
            clock.Now);

        if (registration.IsFailure)
        {
            return registration.Error;
        }

        var transaction = registration.Value;

        repository.Add(transaction);
        outbox.Enqueue(ToIntegrationEvent(transaction), TransactionRegisteredIntegrationEvent.EventName);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponse.From(transaction);
    }

    private static TransactionRegisteredIntegrationEvent ToIntegrationEvent(Transaction transaction) => new(
        EventId: Guid.NewGuid(),
        TransactionId: transaction.Id,
        Type: transaction.Type.ToString(),
        Amount: transaction.Amount.Value,
        ReferenceDate: transaction.ReferenceDate,
        OccurredAt: transaction.CreatedAt);
}
