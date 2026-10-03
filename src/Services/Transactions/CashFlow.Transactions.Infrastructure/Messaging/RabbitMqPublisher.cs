using System.Text;
using CashFlow.Contracts;
using CashFlow.Transactions.Infrastructure.Outbox;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CashFlow.Transactions.Infrastructure.Messaging;

/// <summary>
/// Publica mensagens no RabbitMQ com <b>publisher confirms</b> e mensagens persistentes:
/// a mensagem só é marcada como processada no outbox depois que o broker confirma o recebimento.
/// A conexão é criada sob demanda e recriada se cair (resiliência a reinícios do broker).
/// Publica com <c>mandatory: true</c>: se não houver fila ligada ao exchange (ex.: o Consolidado
/// ainda não subiu nenhuma vez), o broker devolve a mensagem e ela permanece pendente no outbox,
/// em vez de ser descartada silenciosamente.
/// </summary>
internal sealed class RabbitMqPublisher(IOptions<RabbitMqOptions> options) : IIntegrationEventPublisher, IDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly object _sync = new();
    private IConnection? _connection;
    private IModel? _channel;
    private volatile bool _returned;

    public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var channel = EnsureChannel();

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = message.Id.ToString();
            properties.Type = message.EventName;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds());

            _returned = false;
            channel.BasicPublish(
                exchange: MessagingTopology.TransactionsExchange,
                routingKey: message.EventName,
                mandatory: true,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(message.Payload));

            // Lança exceção se o broker der NACK ou não confirmar no tempo limite.
            channel.WaitForConfirmsOrDie(_options.PublishConfirmTimeout);

            // O basic.return chega antes do ack do broker; aqui o flag já está atualizado.
            if (_returned)
            {
                throw new InvalidOperationException(
                    $"Mensagem {message.Id} não roteada: nenhuma fila ligada a '{MessagingTopology.TransactionsExchange}'.");
            }
        }

        return Task.CompletedTask;
    }

    private IModel EnsureChannel()
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        DisposeConnection();

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            RequestedConnectionTimeout = _options.ConnectionTimeout,
            AutomaticRecoveryEnabled = true
        };

        _connection = factory.CreateConnection("cashflow-transactions-outbox");
        _channel = _connection.CreateModel();
        _channel.ConfirmSelect();
        _channel.BasicReturn += (_, _) => _returned = true;
        _channel.ExchangeDeclare(MessagingTopology.TransactionsExchange, ExchangeType.Topic, durable: true, autoDelete: false);

        return _channel;
    }

    private void DisposeConnection()
    {
        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch
        {
            // Conexão já estava quebrada; nada a fazer.
        }

        _channel = null;
        _connection = null;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            DisposeConnection();
        }
    }
}
