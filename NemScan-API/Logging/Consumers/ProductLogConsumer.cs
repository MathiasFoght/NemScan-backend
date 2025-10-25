using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NemScan_API.Config;
using NemScan_API.Models.Events;
using NemScan_API.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NemScan_API.Logging.Consumers;

public class ProductLogConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConfig _config;
    private IConnection? _connection;
    private IModel? _channel;

    public ProductLogConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqConfig> config)
    {
        _scopeFactory = scopeFactory;
        _config = config.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Starting ProductLogConsumer");

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_config.Uri),
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_config.Exchange, ExchangeType.Topic, durable: true);

        _channel.QueueDeclare(_config.ProductScanQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_config.ProductScanQueue, _config.Exchange, "product.#");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            Console.WriteLine("ProductLogConsumer received message");
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                var logEvent = JsonSerializer.Deserialize<ProductScanLogEvent>(json);
                if (logEvent == null)
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NemScanDbContext>();

                db.ProductScanLogs.Add(new ProductScanLogEvent
                {
                    DeviceId = logEvent.DeviceId,
                    ProductNumber = logEvent.ProductNumber,
                    ProductName = logEvent.ProductName,
                    ProductGroup = logEvent.ProductGroup,
                    Success = logEvent.Success,
                    UserRole = logEvent.UserRole,
                    Timestamp = logEvent.Timestamp
                });
                
                await db.SaveChangesAsync(stoppingToken);
                Console.WriteLine("Saved to database successfully");
                
                Console.WriteLine($"Saved scan log for product ({logEvent.ProductNumber}) by {logEvent.UserRole}");
                
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(_config.ProductScanQueue, autoAck: false, consumer);
        Console.WriteLine("ProductLogConsumer started and waiting for messages");

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}