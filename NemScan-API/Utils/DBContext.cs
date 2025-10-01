using Microsoft.EntityFrameworkCore;
using NemScan_API.Models;

namespace NemScan_API.Utils;

public class NemScanDbContext : DbContext
{
    public NemScanDbContext(DbContextOptions<NemScanDbContext> options) : base(options) {}

    public DbSet<User> Users { get; set; }
}