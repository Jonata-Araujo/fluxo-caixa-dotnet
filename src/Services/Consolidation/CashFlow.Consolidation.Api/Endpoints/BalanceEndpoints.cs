using CashFlow.Consolidation.Application.Balances.GetDaily;
using CashFlow.Consolidation.Application.Balances.GetReport;
using CashFlow.Consolidation.Domain;
using CashFlow.SharedKernel;
using CashFlow.Web.Common;

namespace CashFlow.Consolidation.Api.Endpoints;

public static class BalanceEndpoints
{
    public const string RateLimitPolicy = "balances";

    public static IEndpointRouteBuilder MapBalanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/daily-balances")
            .WithTags("Saldo consolidado")
            .RequireRateLimiting(RateLimitPolicy);

        group.MapGet("/{date}", GetDailyAsync)
            .WithName("GetDailyBalance")
            .WithSummary("Saldo consolidado de um dia (yyyy-MM-dd)")
            .Produces<DailyBalanceResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapGet("/", GetReportAsync)
            .WithName("GetBalanceReport")
            .WithSummary("Relatório de saldo consolidado diário no período, com saldo acumulado")
            .Produces<BalanceReport>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }

    private static async Task<IResult> GetDailyAsync(
        DateOnly date,
        IQueryHandler<GetDailyBalanceQuery, DailyBalanceResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetDailyBalanceQuery(date), cancellationToken)).ToOk();

    private static async Task<IResult> GetReportAsync(
        DateOnly from,
        DateOnly to,
        IQueryHandler<GetBalanceReportQuery, BalanceReport> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetBalanceReportQuery(from, to), cancellationToken)).ToOk();
}
