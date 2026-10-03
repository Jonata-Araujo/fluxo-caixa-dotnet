namespace CashFlow.Transactions.Application.Abstractions;

/// <summary>
/// Relógio do negócio: abstrai "agora" e "hoje" no fuso horário do lojista.
/// Mantém regras dependentes de data determinísticas e testáveis.
/// </summary>
public interface IBusinessClock
{
    DateTimeOffset Now { get; }
    DateOnly Today { get; }
}
