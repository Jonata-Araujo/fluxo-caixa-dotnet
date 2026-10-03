using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Cria o schema na subida (com retry). Em produção: migrations versionadas – ver README.</summary>
    public static async Task InitializeConsolidationDatabaseAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 10;
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConsolidationDbContext>();
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
