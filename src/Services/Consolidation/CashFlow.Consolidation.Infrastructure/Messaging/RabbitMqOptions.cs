namespace CashFlow.Consolidation.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>Permite desligar o consumidor (ex.: testes de integração da API de leitura).</summary>
    public bool ConsumerEnabled { get; set; } = true;

    /// <summary>Quantidade de mensagens entregues sem ack por consumidor (backpressure).</summary>
    public ushort PrefetchCount { get; set; } = 50;

    /// <summary>Tentativas em memória para falhas transitórias antes de devolver a mensagem à fila.</summary>
    public int MaxProcessingAttempts { get; set; } = 3;

    /// <summary>Limite de entregas da quorum queue; ao exceder, a mensagem vai para a DLQ.</summary>
    public int DeliveryLimit { get; set; } = 10;

    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
}
