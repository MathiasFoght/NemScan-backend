namespace NemScan_API.Models.DTO.Product;

public class AllProductsDTO
{
    public string ProductNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}