using CashFlow.SharedKernel;
using CashFlow.Transactions.Application.Transactions;
using CashFlow.Transactions.Application.Transactions.GetById;
using CashFlow.Transactions.Application.Transactions.List;
using CashFlow.Transactions.Application.Transactions.Register;
using CashFlow.Web.Common;

namespace CashFlow.Transactions.Api.Endpoints;

/// <summary>Corpo da requisição de registro de lançamento.</summary>
/// <param name="Type">"credit" ou "debit".</param>
/// <param name="Amount">Valor positivo com até 2 casas decimais.</param>
/// <param name="Description">Descrição (até 200 caracteres).</param>
/// <param name="Date">Data de competência (yyyy-MM-dd). Opcional: padrão é hoje.</param>
public sealed record RegisterTransactionRequest(string? Type, decimal Amount, string? Description, DateOnly? Date);

public static class TransactionEndpoints
{
    private const string GetByIdRouteName = "GetTransactionById";

    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/transactions").WithTags("Lançamentos");

        group.MapPost("/", RegisterAsync)
            .WithName("RegisterTransaction")
            .WithSummary("Registra um lançamento de crédito ou débito")
            .Produces<TransactionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName(GetByIdRouteName)
            .WithSummary("Consulta um lançamento pelo identificador")
            .Produces<TransactionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListAsync)
            .WithName("ListTransactions")
            .WithSummary("Lista os lançamentos de um dia (padrão: hoje), paginado")
            .Produces<PagedResponse<TransactionResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterTransactionRequest request,
        ICommandHandler<RegisterTransactionCommand, TransactionResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new RegisterTransactionCommand(request.Type, request.Amount, request.Description, request.Date);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.CreatedAtRoute(GetByIdRouteName, new { id = result.Value.Id }, result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IQueryHandler<GetTransactionByIdQuery, TransactionResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetTransactionByIdQuery(id), cancellationToken)).ToOk();

    private static async Task<IResult> ListAsync(
        DateOnly? date,
        int? page,
        int? pageSize,
        IQueryHandler<ListTransactionsQuery, PagedResponse<TransactionResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListTransactionsQuery(date, page ?? 1, pageSize ?? 50), cancellationToken)).ToOk();
}
