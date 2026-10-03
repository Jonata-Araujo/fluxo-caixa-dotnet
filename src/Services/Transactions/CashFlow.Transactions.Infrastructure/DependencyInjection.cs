using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Infrastructure.Health;
using CashFlow.Transactions.Infrastructure.Messaging;
using CashFlow.Transactions.Infrastructure.Outbox;
using CashFlow.Transactions.Infrastructure.Persistence;
using CashFlow.Transactions.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CashFlow.Transactions.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "TransactionsDb";

    public static IServiceCollection AddTransactionsInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<RabbitMqOptions>().BindConfiguration(RabbitMqOptions.SectionName);
        services.AddOptions<OutboxOptions>().BindConfiguration(OutboxOptions.SectionName);
        services.AddOptions<BusinessClockOptions>().BindConfiguration(BusinessClockOptions.SectionName);

        // Connection string resolvida de forma tardia (no momento da criação do DbContext),
        // o que permite sobrescrevê-la nos testes de integração.
        services.AddDbContext<TransactionsDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' não configurada.");
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TransactionsDbContext>());
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IIntegrationEventOutbox, EfIntegrationEventOutbox>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IBusinessClock, BusinessClock>();

        services.AddSingleton<IIntegrationEventPublisher, RabbitMqPublisher>();
        services.AddScoped<OutboxDispatcher>();
        services.AddHostedService<OutboxProcessor>();

        // Readiness depende apenas do banco: o broker fora do ar NÃO tira o serviço de operação.
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);

        return services;
    }
}
