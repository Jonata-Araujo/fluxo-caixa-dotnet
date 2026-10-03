using CashFlow.SharedKernel;
using CashFlow.Transactions.Application.Transactions;
using CashFlow.Transactions.Application.Transactions.GetById;
using CashFlow.Transactions.Application.Transactions.List;
using CashFlow.Transactions.Application.Transactions.Register;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Transactions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterTransactionCommand, TransactionResponse>, RegisterTransactionHandler>();
        services.AddScoped<IQueryHandler<GetTransactionByIdQuery, TransactionResponse>, GetTransactionByIdHandler>();
        services.AddScoped<IQueryHandler<ListTransactionsQuery, PagedResponse<TransactionResponse>>, ListTransactionsHandler>();

        return services;
    }
}
