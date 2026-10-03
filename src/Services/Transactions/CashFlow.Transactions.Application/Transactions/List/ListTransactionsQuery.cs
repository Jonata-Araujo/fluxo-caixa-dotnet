using CashFlow.SharedKernel;
using CashFlow.Transactions.Application.Abstractions;

namespace CashFlow.Transactions.Application.Transactions.List;

public sealed record ListTransactionsQuery(DateOnly? Date, int Page = 1, int PageSize = 50);

public sealed class ListTransactionsHandler(ITransactionRepository repository, IBusinessClock clock)
    : IQueryHandler<ListTransactionsQuery, PagedResponse<TransactionResponse>>
{
    public const int MaxPageSize = 200;

    public async Task<Result<PagedResponse<TransactionResponse>>> HandleAsync(
        ListTransactionsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1)
        {
            return Error.Validation("pagination.page.invalid", "A página deve ser maior ou igual a 1.");
        }

        if (query.PageSize is < 1 or > MaxPageSize)
        {
            return Error.Validation("pagination.page_size.invalid", $"O tamanho da página deve estar entre 1 e {MaxPageSize}.");
        }

        var date = query.Date ?? clock.Today;
        var (items, total) = await repository.ListByDateAsync(date, query.Page, query.PageSize, cancellationToken);

        return new PagedResponse<TransactionResponse>(
            items.Select(TransactionResponse.From).ToList(),
            query.Page,
            query.PageSize,
            total);
    }
}
