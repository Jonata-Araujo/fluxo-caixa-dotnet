using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.Consolidation.Domain;
using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.Application.Balances.GetDaily;

public sealed record GetDailyBalanceQuery(DateOnly Date);

public sealed record DailyBalanceResponse(
    DateOnly Date,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal NetBalance,
    int TransactionCount,
    DateTimeOffset? UpdatedAt)
{
    public static DailyBalanceResponse From(DailyBalance balance) => new(
        balance.Date,
        balance.TotalCredits,
        balance.TotalDebits,
        balance.NetBalance,
        balance.TransactionCount,
        balance.TransactionCount == 0 ? null : balance.UpdatedAt);
}

/// <summary>
/// Retorna o saldo consolidado do dia. Um dia sem movimentos tem saldo zero
/// (não é tratado como "não encontrado").
/// </summary>
public sealed class GetDailyBalanceHandler(IDailyBalanceReader reader)
    : IQueryHandler<GetDailyBalanceQuery, DailyBalanceResponse>
{
    public async Task<Result<DailyBalanceResponse>> HandleAsync(
        GetDailyBalanceQuery query,
        CancellationToken cancellationToken = default)
    {
        var balance = await reader.GetAsync(query.Date, cancellationToken) ?? DailyBalance.Empty(query.Date);
        return DailyBalanceResponse.From(balance);
    }
}
