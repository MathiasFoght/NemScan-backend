using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NemScan_API.Config;
using NemScan_API.Models.Events;
using NemScan_API.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NemScan_API.Logging.Consumers;

public class ReportLogConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConfig _config;
    private IConnection? _connection;
    private IModel? _channel;

    public ReportLogConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqConfig> config)
    {
        _scopeFactory = scopeFactory;
        _config = config.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Starting ReportLogConsumer");

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_config.Uri),
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_config.Exchange, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(_config.ProductScanReportQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_config.ProductScanReportQueue, _config.Exchange, "report.#");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var logEvent = JsonSerializer.Deserialize<ProductScanReportLogEvent>(json);

                if (logEvent == null)
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NemScanDbContext>();

                db.ProductScanReportLogs.Add(logEvent);
                await db.SaveChangesAsync(stoppingToken);

                _channel.BasicAck(ea.DeliveryTag, false);
                Console.WriteLine("Report saved to database");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing report message: {ex.Message}");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(_config.ProductScanReportQueue, autoAck: false, consumer);
        Console.WriteLine("ReportLogConsumer started and waiting for messages");
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
