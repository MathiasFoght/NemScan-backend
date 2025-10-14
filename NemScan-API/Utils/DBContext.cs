using Microsoft.EntityFrameworkCore;
using NemScan_API.Models;

namespace NemScan_API.Utils;

public class NemScanDbContext : DbContext
{
    public NemScanDbContext(DbContextOptions<NemScanDbContext> options) : base(options) {}

    public DbSet<Employee> Users { get; set; }
    public DbSet<AuthLog> AuthLogs { get; set; }
    public DbSet<EmployeeLog> EmployeeLogs { get; set; }
    public DbSet<ProductScanLog> ProductScanLogs { get; set; }

}