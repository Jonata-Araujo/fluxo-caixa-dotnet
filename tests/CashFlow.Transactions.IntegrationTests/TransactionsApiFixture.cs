using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CashFlow.Transactions.IntegrationTests;

/// <summary>
/// Sobe um PostgreSQL real em container (Testcontainers) e a API em memória (WebApplicationFactory).
/// O RabbitMQ aponta propositalmente para uma porta inexistente: prova que o serviço de
/// lançamentos continua operando com o broker/consolidado fora do ar.
/// Pré-requisito: Docker em execução.
/// </summary>
public sealed class TransactionsApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient CreateClient() => Factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = "cashflow_transactions_tests"
        }.ConnectionString;

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:TransactionsDb", connectionString);
            builder.UseSetting("RabbitMq:HostName", "localhost");
            builder.UseSetting("RabbitMq:Port", "1");
            builder.UseSetting("RabbitMq:ConnectionTimeout", "00:00:01");
            builder.UseSetting("Outbox:PollingInterval", "00:00:00.200");
        });

        // Força a inicialização do host (e a criação do schema).
        _ = Factory.Server;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class TransactionsApiCollection : ICollectionFixture<TransactionsApiFixture>
{
    public const string Name = "transactions-api";
}
