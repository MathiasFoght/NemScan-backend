namespace NemScan_API.Models.DTO.Events;

public class AuthLogEvent
{
    public string EventType { get; set; } = string.Empty;
    public string? EmployeeNumber { get; set; }
    public string? DeviceId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}