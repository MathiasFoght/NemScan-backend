namespace NemScan_API.Models.DTO.Events;

public class EmployeeLogEvent
{
    public string EventType { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}