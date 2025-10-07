
namespace NemScan_API.Models.DTO.Products;

public class EmployeeDTO
{
    public Guid ClientUid { get; set; }
    public Guid Uid { get; set; }
    public string Number { get; set; }
    public string Name { get; set; }

    public decimal CurrentStockQuantity { get; set; }
}