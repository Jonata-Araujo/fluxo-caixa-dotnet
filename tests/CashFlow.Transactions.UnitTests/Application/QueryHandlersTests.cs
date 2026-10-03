using CashFlow.SharedKernel;
using CashFlow.Transactions.Application.Transactions.GetById;
using CashFlow.Transactions.Application.Transactions.List;
using CashFlow.Transactions.Domain;
using CashFlow.Transactions.UnitTests.Fakes;

namespace CashFlow.Transactions.UnitTests.Application;

public sealed class QueryHandlersTests
{
    private static readonly DateOnly Today = new(2026, 9, 29);
    private readonly InMemoryTransactionRepository _repository = new();
    private readonly FixedBusinessClock _clock = new(Today);

    [Fact]
    public async Task GetById_Existing_ShouldReturnTransaction()
    {
        var transaction = Seed(Today, 10m);

        var result = await new GetTransactionByIdHandler(_repository).HandleAsync(new GetTransactionByIdQuery(transaction.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(transaction.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetById_Missing_ShouldReturnNotFound()
    {
        var result = await new GetTransactionByIdHandler(_repository).HandleAsync(new GetTransactionByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task List_ShouldReturnOnlyTransactionsOfRequestedDate_Paged()
    {
        Seed(Today, 1m);
        Seed(Today, 2m);
        Seed(Today, 3m);
        Seed(Today.AddDays(-1), 100m);

        var result = await new ListTransactionsHandler(_repository, _clock)
            .HandleAsync(new ListTransactionsQuery(Today, Page: 1, PageSize: 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.All(result.Value.Items, t => Assert.Equal(Today, t.Date));
    }

    [Fact]
    public async Task List_WithoutDate_ShouldDefaultToToday()
    {
        Seed(Today, 1m);
        Seed(Today.AddDays(-1), 1m);

        var result = await new ListTransactionsHandler(_repository, _clock).HandleAsync(new ListTransactionsQuery(null));

        Assert.Equal(1, result.Value.TotalItems);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, ListTransactionsHandler.MaxPageSize + 1)]
    public async Task List_WithInvalidPagination_ShouldFail(int page, int pageSize)
    {
        var result = await new ListTransactionsHandler(_repository, _clock)
            .HandleAsync(new ListTransactionsQuery(Today, page, pageSize));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    private Transaction Seed(DateOnly date, decimal amount)
    {
        var transaction = Transaction.Register(TransactionType.Credit, amount, "Seed", date, Today, _clock.Now).Value;
        _repository.Add(transaction);
        _repository.Commit();
        return transaction;
    }
}
