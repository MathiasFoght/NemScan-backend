namespace NemScan_API.Models;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? DeviceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}