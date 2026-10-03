using CashFlow.Consolidation.Application.Abstractions;
using CashFlow.Consolidation.Domain;
using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.Application.Balances.GetReport;

public sealed record GetBalanceReportQuery(DateOnly From, DateOnly To);

/// <summary>Relatório de saldo consolidado diário em um período, com saldo acumulado.</summary>
public sealed class GetBalanceReportHandler(IDailyBalanceReader reader)
    : IQueryHandler<GetBalanceReportQuery, BalanceReport>
{
    public async Task<Result<BalanceReport>> HandleAsync(
        GetBalanceReportQuery query,
        CancellationToken cancellationToken = default)
    {
        // Valida antes de tocar no banco (fail fast).
        var validation = BalanceReport.ValidatePeriod(query.From, query.To);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        var opening = await reader.GetAccumulatedBalanceBeforeAsync(query.From, cancellationToken);
        var balances = await reader.ListAsync(query.From, query.To, cancellationToken);

        return BalanceReport.Build(query.From, query.To, opening, balances);
    }
}
