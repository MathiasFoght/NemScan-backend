namespace NemScan_API.Models;

public class Product
{
    public Guid ClientUid { get; set; }
    public Guid Uid { get; set; }
    public string? Number { get; set; }
    public string? Name { get; set; }
    public string? ReceiptText { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public bool DemandSerial { get; set; }
    public bool DemandSeller { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid DisplayProductGroupUid { get; set; }
    public bool IsVirtual { get; set; }
    public bool NoDiscount { get; set; }
    public string? ButtonColor { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeleteDate { get; set; }
    public Guid? FollowProductUid { get; set; }
    public Guid DeliveryTypeUid { get; set; }
    public int? DeliveryTypeId { get; set; }
    public Guid? DeliveryItemUid { get; set; }
    public bool IsVatFree { get; set; }
    public decimal VatPercent { get; set; }
    public string? SupplierNumber { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public int UnitType { get; set; }
    public DateTime? LastTimestamp { get; set; }
    public string? SupplierName { get; set; }
    public Guid? FinancialProductGroupUid { get; set; }
    public DateTime? ErpTimestamp { get; set; }
    public int? SortOrder { get; set; }
    public Guid? BrandUid { get; set; }
    public string? Combination1 { get; set; }
    public string? Combination2 { get; set; }
    public string? Combination3 { get; set; }
    public string? Combination4 { get; set; }
    public bool NoStock { get; set; }
    public Guid? ParentUid { get; set; }
    public bool IsCombinationMaster { get; set; }
    public bool SyncToWeb { get; set; }
    public Guid? WarrantyProfileUid { get; set; }
    public bool InheritPriceMatrixFromMaster { get; set; }
    public decimal CurrentCostPrice { get; set; }
    public decimal CurrentSalesPrice { get; set; }
    public decimal CurrentStockQuantity { get; set; }
    public string? ExternalRef { get; set; }
    public string? Image { get; set; }
    public string? Barcodes { get; set; }
    public string? Variants { get; set; }
    public string? Translations { get; set; }
}
