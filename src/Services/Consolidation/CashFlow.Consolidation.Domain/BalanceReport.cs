using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.Domain;

public sealed record BalanceReportLine(
    DateOnly Date,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal NetBalance,
    int TransactionCount,
    decimal ClosingBalance);

/// <summary>
/// Relatório de saldo consolidado por período: saldo de abertura, uma linha por dia
/// (inclusive dias sem movimento) e saldo acumulado ao fim de cada dia.
/// </summary>
public sealed record BalanceReport(
    DateOnly From,
    DateOnly To,
    decimal OpeningBalance,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal ClosingBalance,
    IReadOnlyList<BalanceReportLine> Days)
{
    public static Result ValidatePeriod(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            return Result.Failure(ConsolidationErrors.InvalidPeriod);
        }

        return to.DayNumber - from.DayNumber + 1 > ConsolidationErrors.MaxReportDays
            ? Result.Failure(ConsolidationErrors.PeriodTooLong)
            : Result.Success();
    }

    /// <param name="openingBalance">Saldo acumulado de todos os dias anteriores a <paramref name="from"/>.</param>
    /// <param name="balances">Saldos persistidos no período (dias sem movimento podem estar ausentes).</param>
    public static Result<BalanceReport> Build(
        DateOnly from,
        DateOnly to,
        decimal openingBalance,
        IEnumerable<DailyBalance> balances)
    {
        var validation = ValidatePeriod(from, to);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        var byDate = balances
            .Where(b => b.Date >= from && b.Date <= to)
            .ToDictionary(b => b.Date);

        var lines = new List<BalanceReportLine>(to.DayNumber - from.DayNumber + 1);
        var running = openingBalance;
        decimal credits = 0, debits = 0;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var balance = byDate.TryGetValue(day, out var found) ? found : DailyBalance.Empty(day);
            running += balance.NetBalance;
            credits += balance.TotalCredits;
            debits += balance.TotalDebits;

            lines.Add(new BalanceReportLine(
                day,
                balance.TotalCredits,
                balance.TotalDebits,
                balance.NetBalance,
                balance.TransactionCount,
                running));
        }

        return new BalanceReport(from, to, openingBalance, credits, debits, running, lines);
    }
}
