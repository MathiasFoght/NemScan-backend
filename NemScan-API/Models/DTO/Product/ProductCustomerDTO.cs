using NemScan_API.Models.DTO.ProductCampaign;

namespace NemScan_API.Models.DTO.Product;

public class ProductCustomerDTO
{
    public Guid Uid { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string ProductNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? CurrentSalesPrice { get; set; }
    public string? ProductGroup { get; set; }
    public List<ProductCampaignDTO>? Campaigns { get; set; }

}