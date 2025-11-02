using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NemScan_API.Models.Auth;
using NemScan_API.Models.Events;

namespace NemScan_API.Utils;

public class NemScanDbContext : DbContext
{
    public NemScanDbContext(DbContextOptions<NemScanDbContext> options) : base(options) {}

    public DbSet<Employee> Users { get; set; }
    public DbSet<AuthLogEvent> AuthLogs { get; set; }
    public DbSet<EmployeeLogEvent> EmployeeLogs { get; set; }
    public DbSet<ProductScanLogEvent> ProductScanLogs { get; set; }
    public DbSet<ProductScanReportLogEvent> ProductScanReportLogs { get; set; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateTimeOffsetToUtcConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            v => v.ToUniversalTime(),   
            v => v                      
        );

        modelBuilder.Entity<ProductScanLogEvent>()
            .Property(p => p.Timestamp)
            .HasConversion(dateTimeOffsetToUtcConverter);
        
        //Event: ProductScanLogEvent
        modelBuilder.Entity<ProductScanLogEvent>()
            .HasIndex(p => p.Timestamp)
            .HasDatabaseName("IX_ProductScanLog_Timestamp");
        
        modelBuilder.Entity<ProductScanLogEvent>()
            .HasIndex(p => new { p.Timestamp, p.ProductGroup })
            .HasDatabaseName("IX_ProductScanLog_Timestamp_ProductGroup");
        
        modelBuilder.Entity<ProductScanLogEvent>()
            .HasIndex(p => new { p.Timestamp, p.ProductNumber })
            .HasDatabaseName("IX_ProductScanLog_Timestamp_ProductNumber");
        
        //Event: ProductScanReportLogEvent
        modelBuilder.Entity<ProductScanReportLogEvent>()
            .HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_ProductScanReport_CreatedAt");
        
        modelBuilder.Entity<ProductScanReportLogEvent>()
            .HasIndex(r => new { r.ProductNumber, r.ProductName })
            .HasDatabaseName("IX_ProductScanReport_ProductNumber_ProductName");
        
        modelBuilder.Entity<ProductScanReportLogEvent>()
            .HasIndex(r => new { r.CreatedAt, r.ProductNumber })
            .HasDatabaseName("IX_ProductScanReport_CreatedAt_ProductNumber");
        
        base.OnModelCreating(modelBuilder);
    }

}