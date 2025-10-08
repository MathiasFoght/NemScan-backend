namespace NemScan_API.Models.DTO.Product;

public class ProductForCustomer
{
    public Guid ClientUid { get; set; }
    public Guid Uid { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}