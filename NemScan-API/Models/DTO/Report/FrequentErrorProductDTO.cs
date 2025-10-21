namespace NemScan_API.Models.DTO.Report;

public class FrequentErrorProductDTO
{
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public int ErrorCount { get; set; }
    public double Percentage { get; set; }
}