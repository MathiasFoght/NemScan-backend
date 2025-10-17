using NemScan_API.Models.DTO.ProductCampaign;

namespace NemScan_API.Models.DTO.Product;

public class ProductEmployeeDTO
{
    public Guid Uid { get; set; }
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductGroup { get; set; }
    public string? ProductBrand { get; set; }
    public decimal CurrentStockQuantity { get; set; }
    public decimal CurrentSalesPrice { get; set; }
    public List<ProductCampaignDTO>? Campaigns { get; set; }

}