using NemScan_API.Interfaces;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NemScan_API.Config;
using RabbitMQ.Client;
using RabbitMQClientModel = RabbitMQ.Client.IModel;

namespace NemScan_API.Logging.Publisher;

public class RabbitMqPublisher : ILogEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly RabbitMQClientModel _channel;
    private readonly RabbitMqConfig _config;

    public RabbitMqPublisher(IOptions<RabbitMqConfig> config)
    {
        _config = config.Value;
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_config.Uri),
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange: _config.Exchange, type: ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync<T>(T logEvent, string routingKey)
    {
        var json = JsonSerializer.Serialize(logEvent);
        var body = Encoding.UTF8.GetBytes(json);

        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        
        _channel.BasicPublish(
            exchange: _config.Exchange,
            routingKey: routingKey,
            basicProperties: props,
            body: body
        );
        
        Console.WriteLine($"Publisher published to exchange '{_config.Exchange}' with routing key '{routingKey}'");

        Console.WriteLine($"Publisher sent: {routingKey}");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}