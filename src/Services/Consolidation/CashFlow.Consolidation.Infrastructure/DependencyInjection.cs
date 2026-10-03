using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.Consolidation.Infrastructure.Caching;
using CashFlow.Consolidation.Infrastructure.Health;
using CashFlow.Consolidation.Infrastructure.Messaging;
using CashFlow.Consolidation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CashFlow.Consolidation.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ConsolidationDb";

    public static IServiceCollection AddConsolidationInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<RabbitMqOptions>().BindConfiguration(RabbitMqOptions.SectionName);
        services.AddOptions<BalanceCacheOptions>().BindConfiguration(BalanceCacheOptions.SectionName);

        services.AddDbContext<ConsolidationDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' não configurada.");
            options.UseNpgsql(connectionString);
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddMemoryCache();

        services.AddScoped<IDailyBalanceProjection, DailyBalanceProjection>();

        // Decorator registrado manualmente: leitura → cache → banco.
        services.AddScoped<DailyBalanceReader>();
        services.AddScoped<IDailyBalanceReader>(sp =>
        {
            var reader = sp.GetRequiredService<DailyBalanceReader>();
            var cacheOptions = sp.GetRequiredService<IOptions<BalanceCacheOptions>>();
            return cacheOptions.Value.Enabled
                ? new CachedDailyBalanceReader(reader, sp.GetRequiredService<IMemoryCache>(), cacheOptions)
                : reader;
        });

        services.AddSingleton<TransactionRegisteredMessageHandler>();
        services.AddHostedService<TransactionRegisteredConsumer>();

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);

        return services;
    }
}
