using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashFlow.Transactions.Infrastructure.Outbox;

/// <summary>Worker em background que drena o outbox periodicamente.</summary>
internal sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Outbox processor desabilitado por configuração.");
            return;
        }

        using var timer = new PeriodicTimer(settings.PollingInterval);
        try
        {
            do
            {
                await DrainAsync(settings.BatchSize, stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Encerramento normal da aplicação.
        }
    }

    private async Task DrainAsync(int batchSize, CancellationToken stoppingToken)
    {
        try
        {
            int published;
            do
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
                published = await dispatcher.DispatchPendingAsync(stoppingToken);
            }
            while (published == batchSize && !stoppingToken.IsCancellationRequested);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Erro ao processar o outbox. Nova tentativa no próximo ciclo.");
        }
    }
}
