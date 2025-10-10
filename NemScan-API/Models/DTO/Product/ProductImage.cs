namespace NemScan_API.Models.DTO.Product;

public class ProductImage
{
    public Guid ProductUid { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
