using System.ComponentModel.DataAnnotations;

namespace NemScan_API.Models;

public enum UserRole
{
    Admin,
    Basic
}

public class Employee
{
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public string EmployeeNumber { get; set; } = string.Empty;
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public UserRole Role { get; set; } = UserRole.Basic;
    
    [RegularExpression(@"^\d{6}$", ErrorMessage = "StoreNumber must be exactly 6 digits.")]
    [Required]
    public string StoreNumber { get; set; } = string.Empty;
    
    public string? ProfileImageUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}