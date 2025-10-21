using System.ComponentModel.DataAnnotations;

namespace NemScan_API.Models.Auth;

public class Customer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? DeviceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}