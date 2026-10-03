using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.Domain;

public static class ConsolidationErrors
{
    public const int MaxReportDays = 366;

    public static readonly Error InvalidTransactionId =
        Error.Validation("movement.transaction_id.empty", "O identificador do lançamento é obrigatório.");

    public static readonly Error AmountMustBePositive =
        Error.Validation("movement.amount.non_positive", "O valor do movimento deve ser maior que zero.");

    public static readonly Error InvalidPeriod =
        Error.Validation("report.period.invalid", "A data inicial deve ser menor ou igual à data final.");

    public static readonly Error PeriodTooLong =
        Error.Validation("report.period.too_long", $"O período do relatório deve ter no máximo {MaxReportDays} dias.");
}
