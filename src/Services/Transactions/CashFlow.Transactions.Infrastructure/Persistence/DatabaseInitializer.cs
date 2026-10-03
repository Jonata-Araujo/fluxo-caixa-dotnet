using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CashFlow.Transactions.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Cria o schema na subida da aplicação (com retry, pois no docker-compose o banco pode
    /// ainda estar iniciando). Em produção o ideal é aplicar migrations versionadas em um
    /// passo separado do pipeline (ex.: EF migrations bundle) – ver README.
    /// </summary>
    public static async Task InitializeTransactionsDatabaseAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 10;
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Banco indisponível (tentativa {Attempt}/{Max}). Nova tentativa em 3s.", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }
}
