namespace NemScan_API.Models.DTO.ProductCampaign;

public class ProductCampaignDTO
{
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? ActivateAtQuantity { get; set; }
    public decimal? DiscountInPercentage { get; set; }
    public List<string> ProductsInCampaign { get; set; } = new();
}