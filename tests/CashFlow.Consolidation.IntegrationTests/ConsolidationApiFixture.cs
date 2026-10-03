using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.Consolidation.Domain;
using CashFlow.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CashFlow.Consolidation.IntegrationTests;

/// <summary>
/// PostgreSQL real (Testcontainers) + API em memória. O consumidor RabbitMQ é desligado:
/// os testes alimentam a projeção diretamente, isolando a lógica de persistência do transporte.
/// Pré-requisito: Docker em execução.
/// </summary>
public sealed class ConsolidationApiFixture : IAsyncLifetime
{
    private static int _dayCounter;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>Gera datas únicas por teste, evitando interferência entre testes no mesmo banco.</summary>
    public static DateOnly UniqueDay() => new DateOnly(2030, 1, 1).AddDays(Interlocked.Increment(ref _dayCounter) * 10);

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = "cashflow_consolidation_tests",
            MaxPoolSize = 50
        }.ConnectionString;

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:ConsolidationDb", connectionString);
            builder.UseSetting("RabbitMq:ConsumerEnabled", "false");
            builder.UseSetting("BalanceCache:Enabled", "false");
        });

        _ = Factory.Server;
    }

    public async Task<bool> ApplyAsync(DateOnly date, TransactionType type, decimal amount, Guid? transactionId = null)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var projection = scope.ServiceProvider.GetRequiredService<IDailyBalanceProjection>();
        var movement = BalanceMovement.Create(transactionId ?? Guid.NewGuid(), date, type, amount).Value;
        return await projection.TryApplyAsync(movement);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ConsolidationApiCollection : ICollectionFixture<ConsolidationApiFixture>
{
    public const string Name = "consolidation-api";
}
