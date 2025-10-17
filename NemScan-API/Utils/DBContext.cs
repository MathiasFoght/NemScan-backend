using Microsoft.EntityFrameworkCore;
using NemScan_API.Models;
using NemScan_API.Models.Events;

namespace NemScan_API.Utils;

public class NemScanDbContext : DbContext
{
    public NemScanDbContext(DbContextOptions<NemScanDbContext> options) : base(options) {}

    public DbSet<Employee> Users { get; set; }
    public DbSet<AuthLogEvent> AuthLogs { get; set; }
    public DbSet<EmployeeLogEvent> EmployeeLogs { get; set; }
    public DbSet<ProductScanLogEvent> ProductScanLogs { get; set; }

}