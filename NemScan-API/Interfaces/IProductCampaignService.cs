using NemScan_API.Models.DTO.ProductCampaign;

namespace NemScan_API.Interfaces;

public interface IProductCampaignService
{
    Task<List<ProductCampaignDTO>> GetAvailableCampaignsAsync();
}