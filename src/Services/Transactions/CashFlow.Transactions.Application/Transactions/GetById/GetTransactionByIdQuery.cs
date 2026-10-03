using CashFlow.SharedKernel;
using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Domain;

namespace CashFlow.Transactions.Application.Transactions.GetById;

public sealed record GetTransactionByIdQuery(Guid Id);

public sealed class GetTransactionByIdHandler(ITransactionRepository repository)
    : IQueryHandler<GetTransactionByIdQuery, TransactionResponse>
{
    public async Task<Result<TransactionResponse>> HandleAsync(
        GetTransactionByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.GetByIdAsync(query.Id, cancellationToken);

        return transaction is null
            ? TransactionErrors.NotFound(query.Id)
            : TransactionResponse.From(transaction);
    }
}
