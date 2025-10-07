namespace NemScan_API.Models.DTO;

public class EmployeeProfileDTO
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string StoreNumber { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
}