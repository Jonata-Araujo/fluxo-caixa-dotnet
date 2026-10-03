namespace CashFlow.Transactions.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan PublishConfirmTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
