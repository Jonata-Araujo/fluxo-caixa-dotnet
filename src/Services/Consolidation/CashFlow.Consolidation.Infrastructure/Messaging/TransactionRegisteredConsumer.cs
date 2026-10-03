using CashFlow.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CashFlow.Consolidation.Infrastructure.Messaging;

/// <summary>
/// Consumidor RabbitMQ do evento <c>transaction.registered</c>.
/// <list type="bullet">
///   <item>Ack manual: a mensagem só sai da fila depois de persistida no saldo.</item>
///   <item>Prefetch: limita mensagens em voo (backpressure) – picos ficam represados na fila,
///         não derrubam o banco.</item>
///   <item>Quorum queue com <c>x-delivery-limit</c>: mensagens que falham repetidamente vão para a DLQ.</item>
///   <item>Reconexão automática: se o broker cair, o consumo é retomado sozinho.</item>
/// </list>
/// </summary>
internal sealed class TransactionRegisteredConsumer(
    TransactionRegisteredMessageHandler messageHandler,
    IOptions<RabbitMqOptions> options,
    ILogger<TransactionRegisteredConsumer> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IModel? _channel;
    private CancellationToken _stoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.ConsumerEnabled)
        {
            logger.LogInformation("Consumidor RabbitMQ desabilitado por configuração.");
            return;
        }

        _stoppingToken = stoppingToken;

        // Não bloqueia a subida da API enquanto o broker não responde.
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                StartConsuming();
                logger.LogInformation("Consumindo a fila {Queue}.", MessagingTopology.ConsolidationQueue);
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RabbitMQ indisponível. Nova tentativa em {Delay}.", _options.ReconnectDelay);
                CloseConnection();
                try
                {
                    await Task.Delay(_options.ReconnectDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private void StartConsuming()
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = _options.ReconnectDelay
        };

        _connection = factory.CreateConnection("cashflow-consolidation-consumer");
        _channel = _connection.CreateModel();

        DeclareTopology(_channel, _options.DeliveryLimit);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: _options.PrefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;
        _channel.BasicConsume(queue: MessagingTopology.ConsolidationQueue, autoAck: false, consumer: consumer);
    }

    internal static void DeclareTopology(IModel channel, int deliveryLimit)
    {
        channel.ExchangeDeclare(MessagingTopology.TransactionsExchange, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(MessagingTopology.TransactionsDeadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false);

        channel.QueueDeclare(MessagingTopology.ConsolidationDeadLetterQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueBind(MessagingTopology.ConsolidationDeadLetterQueue, MessagingTopology.TransactionsDeadLetterExchange, routingKey: string.Empty);

        channel.QueueDeclare(
            MessagingTopology.ConsolidationQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-queue-type"] = "quorum",
                ["x-delivery-limit"] = deliveryLimit,
                ["x-dead-letter-exchange"] = MessagingTopology.TransactionsDeadLetterExchange
            });
        channel.QueueBind(MessagingTopology.ConsolidationQueue, MessagingTopology.TransactionsExchange,
            routingKey: MessagingTopology.TransactionRegisteredRoutingKey);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs delivery)
    {
        var channel = ((AsyncEventingBasicConsumer)sender).Model;

        MessageDisposition disposition;
        try
        {
            disposition = await messageHandler.HandleAsync(delivery.Body, _stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Aplicação encerrando: devolve a mensagem para outra instância processar.
            disposition = MessageDisposition.Requeue;
        }

        switch (disposition)
        {
            case MessageDisposition.Ack:
                channel.BasicAck(delivery.DeliveryTag, multiple: false);
                break;
            case MessageDisposition.Requeue:
                channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
                break;
            default:
                channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
                break;
        }
    }

    private void CloseConnection()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch
        {
            // Ignora: conexão já fechada/quebrada.
        }

        _channel?.Dispose();
        _connection?.Dispose();
        _channel = null;
        _connection = null;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        CloseConnection();
    }
}
