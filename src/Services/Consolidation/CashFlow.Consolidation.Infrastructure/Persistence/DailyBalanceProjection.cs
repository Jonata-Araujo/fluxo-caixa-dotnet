using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.Consolidation.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Persistence;

/// <summary>
/// Atualiza o saldo diário de forma <b>atômica, idempotente e segura sob concorrência</b>:
/// <list type="number">
///   <item>Insere o id do lançamento em <c>processed_transactions</c> (ON CONFLICT DO NOTHING).
///         Se já existia, a mensagem é duplicada e nada mais é feito.</item>
///   <item>Faz um upsert incremental em <c>daily_balances</c> (ON CONFLICT DO UPDATE SET x = x + delta).
///         O incremento ocorre no banco, sem ler-modificar-escrever na aplicação, então N consumidores
///         em paralelo nunca perdem atualização (sem lost update e sem retry por concorrência otimista).</item>
/// </list>
/// Ambas as instruções rodam na mesma transação.
/// </summary>
internal sealed class DailyBalanceProjection(ConsolidationDbContext dbContext, TimeProvider timeProvider)
    : IDailyBalanceProjection
{
    public async Task<bool> TryApplyAsync(BalanceMovement movement, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var inserted = await dbContext.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO processed_transactions (transaction_id, processed_at)
             VALUES ({movement.TransactionId}, {now})
             ON CONFLICT (transaction_id) DO NOTHING
             """,
            cancellationToken);

        if (inserted == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await dbContext.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO daily_balances (balance_date, total_credits, total_debits, transaction_count, updated_at)
             VALUES ({movement.Date}, {movement.CreditAmount}, {movement.DebitAmount}, 1, {now})
             ON CONFLICT (balance_date) DO UPDATE SET
                 total_credits     = daily_balances.total_credits + EXCLUDED.total_credits,
                 total_debits      = daily_balances.total_debits + EXCLUDED.total_debits,
                 transaction_count = daily_balances.transaction_count + 1,
                 updated_at        = EXCLUDED.updated_at
             """,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
