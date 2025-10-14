namespace NemScan_API.Interfaces;

public interface ILogEventPublisher
{
    Task PublishAsync<T>(T logEvent, string routingKey);
}