using System.ComponentModel.DataAnnotations;

namespace NemScan_API.Models;

public class AuthLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public string EventType { get; set; } = string.Empty;
    public string? EmployeeNumber { get; set; }
    public string? DeviceId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}