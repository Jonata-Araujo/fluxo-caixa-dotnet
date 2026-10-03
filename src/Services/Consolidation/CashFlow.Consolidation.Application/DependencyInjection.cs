using CashFlow.Consolidation.Application.Balances.ApplyTransaction;
using CashFlow.Consolidation.Application.Balances.GetDaily;
using CashFlow.Consolidation.Application.Balances.GetReport;
using CashFlow.Consolidation.Domain;
using CashFlow.Contracts;
using CashFlow.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Consolidation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddConsolidationApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<TransactionRegisteredIntegrationEvent, ProcessingOutcome>, ApplyTransactionToBalanceHandler>();
        services.AddScoped<IQueryHandler<GetDailyBalanceQuery, DailyBalanceResponse>, GetDailyBalanceHandler>();
        services.AddScoped<IQueryHandler<GetBalanceReportQuery, BalanceReport>, GetBalanceReportHandler>();

        return services;
    }
}
