using System.ComponentModel.DataAnnotations;

namespace NemScan_API.Models;

public class Customer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string? DeviceId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}