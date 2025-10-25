using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NemScan_API.Config;
using NemScan_API.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using EmployeeLogEvent = NemScan_API.Models.Events.EmployeeLogEvent;

namespace NemScan_API.Logging.Consumers;

public class EmployeeLogConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConfig _config;
    private IConnection? _connection;
    private IModel? _channel;

    public EmployeeLogConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqConfig> config)
    {
        _scopeFactory = scopeFactory;
        _config = config.Value;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Console.WriteLine("Starting EmployeeLogConsumer");

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_config.Uri),
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(_config.Exchange, ExchangeType.Topic, durable: true);

            _channel.QueueDeclare(_config.EmployeeQueue, durable: true, exclusive: false, autoDelete: false);

            _channel.QueueBind(_config.EmployeeQueue, _config.Exchange, "employee.#");

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                Console.WriteLine("EmployeeLogConsumer received message");
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                    var logEvent = JsonSerializer.Deserialize<EmployeeLogEvent>(json);
                    if (logEvent == null)
                    {
                        _channel?.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<NemScanDbContext>();

                    db.EmployeeLogs.Add(new EmployeeLogEvent
                    {
                        EventType = logEvent.EventType,
                        EmployeeNumber = logEvent.EmployeeNumber,
                        Success = logEvent.Success,
                        Message = logEvent.Message,
                        Timestamp = logEvent.Timestamp
                    });
                    
                    await db.SaveChangesAsync(stoppingToken);
                    Console.WriteLine("Saved to database successfully");
                    
                    Console.WriteLine($"Employee log saved for ({logEvent.EmployeeNumber}) ({logEvent.EventType})");
                    
                    _channel?.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                    _channel?.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(_config.EmployeeQueue, autoAck: false, consumer);
            Console.WriteLine("EmployeeLogConsumer started and waiting for messages");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start consumer. Error: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}