using CashFlow.Transactions.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace CashFlow.Transactions.Infrastructure.Time;

public sealed class BusinessClockOptions
{
    public const string SectionName = "BusinessClock";

    /// <summary>Fuso horário do lojista (IANA). Define a virada do "dia do caixa".</summary>
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";
}

internal sealed class BusinessClock(TimeProvider timeProvider, IOptions<BusinessClockOptions> options) : IBusinessClock
{
    private readonly TimeZoneInfo _timeZone = ResolveTimeZone(options.Value.TimeZoneId);

    public DateTimeOffset Now => timeProvider.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Now, _timeZone).DateTime);

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
