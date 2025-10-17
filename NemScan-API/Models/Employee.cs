using System.ComponentModel.DataAnnotations;

namespace NemScan_API.Models;

public enum EmployeeRole
{
    Admin,
    Basic
}

public enum EmployeePosition
{
    ServiceAssistant,
    StoreManager      
}

public class Employee
{
    [Key]
    public Guid Id { get; set; }
    
    public string EmployeeNumber { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public EmployeeRole Role { get; set; } = EmployeeRole.Basic;
    
    public EmployeePosition Position { get; set; } = EmployeePosition.ServiceAssistant;

    
    [RegularExpression(@"^\d{6}$", ErrorMessage = "StoreNumber must be exactly 6 digits.")]
    public string StoreNumber { get; set; } = string.Empty;
    
    public string? ProfileImageUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsValidPosition()
    {
        return Role switch
        {
            EmployeeRole.Basic when Position == EmployeePosition.ServiceAssistant => true,
            EmployeeRole.Admin when Position == EmployeePosition.StoreManager => true,
            _ => false
        };
    }
}