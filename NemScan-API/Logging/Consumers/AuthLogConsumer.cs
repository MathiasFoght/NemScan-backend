using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NemScan_API.Config;
using NemScan_API.Utils;
using RabbitMQ.Client;
using RabbitMQClientModel = RabbitMQ.Client.IModel;
using RabbitMQ.Client.Events;
using AuthLogEvent = NemScan_API.Models.Events.AuthLogEvent;

namespace NemScan_API.Logging.Consumers;

public class AuthLogConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConfig _config;
    private IConnection? _connection;
    private RabbitMQClientModel? _channel;
    
    public AuthLogConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqConfig> config)
    {
        _scopeFactory = scopeFactory;
        _config = config.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Console.WriteLine("Starting AuthLogConsumer");
            
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_config.Uri),
                DispatchConsumersAsync = true
            };
            
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _channel.ExchangeDeclare(_config.Exchange, ExchangeType.Topic, durable: true);

            _channel.QueueDeclare(_config.AuthQueue, durable: true, exclusive: false, autoDelete: false);

            _channel.QueueBind(_config.AuthQueue, _config.Exchange, "auth.#");

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                Console.WriteLine("AuthLogConsumer received message");
                
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    
                    var logEvent = JsonSerializer.Deserialize<AuthLogEvent>(json);
                    if (logEvent == null)
                    {
                        _channel?.BasicAck(ea.DeliveryTag, false);
                        return;
                    }
                    
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<NemScanDbContext>();

                    db.AuthLogs.Add(new AuthLogEvent
                    {
                        EventType = logEvent.EventType,
                        EmployeeNumber = logEvent.EmployeeNumber,
                        DeviceId = logEvent.DeviceId,
                        Success = logEvent.Success,
                        Message = logEvent.Message,
                        Timestamp = logEvent.Timestamp
                    });
                    
                    await db.SaveChangesAsync(stoppingToken);
                    Console.WriteLine("Saved to database successfully");
                    
                    Console.WriteLine($"Auth log saved for ({logEvent.EmployeeNumber}) ({logEvent.EventType})");

                    _channel?.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                    _channel?.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(_config.AuthQueue, autoAck: false, consumer);
            Console.WriteLine("AuthLogConsumer started and waiting for messages");
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
