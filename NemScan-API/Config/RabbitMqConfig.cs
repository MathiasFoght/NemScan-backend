namespace NemScan_API.Config;

public class RabbitMqConfig
{
    public string Uri { get; set; } = string.Empty;
    public string Exchange { get; set; } = "nemscan.events";
    public string AuthQueue { get; set; } = "nemscan.auth.logger";
    public string EmployeeQueue { get; set; } = "nemscan.employee.logger";
    public string ProductScanQueue { get; set; } = "nemscan.productScan.logger";

}
