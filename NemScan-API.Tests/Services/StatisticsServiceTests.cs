using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using NemScan_API.Interfaces;
using NemScan_API.Models.Events;
using NemScan_API.Services.Statistics;
using NemScan_API.Utils;
using NUnit.Framework;

namespace NemScan_API.Tests.Services;

[TestFixture]
public class StatisticsServiceTests
{
    private NemScanDbContext _db = null!;
    private StatisticsService _service = null!;
    private Mock<IAmeroAuthService> _authMock = null!;
    private Mock<HttpMessageHandler> _httpHandlerMock = null!;
    private HttpClient _httpClient = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<NemScanDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new NemScanDbContext(options);

        _authMock = new Mock<IAmeroAuthService>();

        _httpHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_httpHandlerMock.Object);

        _service = new StatisticsService(_db, _httpClient, _authMock.Object);
    }

    // ---------------------- SCAN PERFORMANCE ----------------------

    [Test]
    public async Task GetScanPerformance_ReturnsCorrectCountsAndRate()
    {
        _db.ProductScanLogs.AddRange(
            new ProductScanLogEvent { Success = true, Timestamp = DateTimeOffset.UtcNow },
            new ProductScanLogEvent { Success = false, Timestamp = DateTimeOffset.UtcNow }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetScanPerformanceAsync();

        Assert.That(result.TotalScans, Is.EqualTo(2)); 
        Assert.That(result.SuccessfulScans, Is.EqualTo(1));
        Assert.That(result.FailedScans, Is.EqualTo(1));
        Assert.That(result.SuccessRate, Is.EqualTo(50.0));
    }

    // ---------------------- PRODUCT GROUP DISTRIBUTION ----------------------

    [Test]
    public async Task GetProductGroupDistribution_GroupsCorrectly()
    {
        _db.ProductScanLogs.AddRange(
            new ProductScanLogEvent { ProductGroup = "Fruit", Timestamp = DateTimeOffset.UtcNow },
            new ProductScanLogEvent { ProductGroup = "Fruit", Timestamp = DateTimeOffset.UtcNow },
            new ProductScanLogEvent { ProductGroup = "Bakery", Timestamp = DateTimeOffset.UtcNow }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetProductGroupDistributionAsync();

        Assert.That(result.Count, Is.EqualTo(2));

        var fruit = result.First(x => x.ProductGroup == "Fruit");
        Assert.That(fruit.ScanCount, Is.EqualTo(2));
        Assert.That(fruit.Percentage, Is.EqualTo(66.7).Within(0.1));
    }

    // ---------------------- MOST SCANNED PRODUCT ----------------------

    [Test]
    public async Task GetMostScannedProduct_ReturnsHighestCount()
    {
        var now = DateTimeOffset.UtcNow;

        _db.ProductScanLogs.AddRange(
            new ProductScanLogEvent { ProductNumber = "A", ProductName = "Apple", Timestamp = now },
            new ProductScanLogEvent { ProductNumber = "A", ProductName = "Apple", Timestamp = now },
            new ProductScanLogEvent { ProductNumber = "B", ProductName = "Banana", Timestamp = now }
        );

        await _db.SaveChangesAsync();

        var result = await _service.GetMostScannedProductAsync();

        Assert.NotNull(result);
        Assert.That(result!.ProductNumber, Is.EqualTo("A"));
        Assert.That(result.ScanCount, Is.EqualTo(2));
    }

    // ---------------------- LOW STOCK PRODUCTS ----------------------

    [Test]
    public async Task GetLowStockProducts_ReturnsProductsBelowThreshold()
    {
        // Mock token
        _authMock.Setup(a => a.GetAccessTokenAsync()).ReturnsAsync("dummy-token");

        // Mock API response
        var json = """
        {
            "Items": [
                { "Number": "1000", "Name": "Milk", "DisplayProductGroupUid": "123", "CurrentStockQuantity": 50 },
                { "Number": "2000", "Name": "Cheese", "DisplayProductGroupUid": "123", "CurrentStockQuantity": 200 }
            ]
        }
        """;

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var result = await _service.GetLowStockProductsAsync(minThreshold: 100);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].ProductNumber, Is.EqualTo("1000"));
    }
    
    // ---------------------- SCAN ACTIVITY - Heatmap generation----------------------
    
    [Test]
    public async Task GetScanActivity_Week_GeneratesCorrectHeatmap()
    {
        var copenhagen = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

        // Find Monday of the current week
        var now = DateTime.UtcNow;
        int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = now.AddDays(-diff).Date;

        // Create a Monday 09:00 Copenhagen time
        var mondayMorningCph = new DateTimeOffset(
            new DateTime(monday.Year, monday.Month, monday.Day, 9, 0, 0),
            copenhagen.BaseUtcOffset
        );

        var mondayMorningUtc = mondayMorningCph.ToUniversalTime();

        _db.ProductScanLogs.Add(new ProductScanLogEvent
        {
            Timestamp = mondayMorningUtc,
            ProductName = "Test",
            ProductNumber = "1010"
        });

        await _db.SaveChangesAsync();

        var result = await _service.GetScanActivityAsync("week");

        Assert.That(result.PeriodType, Is.EqualTo("week"));
        Assert.That(result.Heatmap.Count, Is.EqualTo(28));

        var mondayEntry = result.Heatmap
            .First(x => x.Day == "Monday" && x.Period == "Morning");

        Assert.That(mondayEntry.Count, Is.EqualTo(1));
    }
    
    // ---------------------- SCAN ACTIVITY - Rolling 7-day weighted average ----------------------
    
    [Test]
    public async Task GetScanActivity_Month_ComputesRollingWeightedAverage()
    {
        var start = DateTimeOffset.UtcNow.Date.AddDays(-10);

        for (int i = 0; i < 10; i++)
        {
            _db.ProductScanLogs.Add(new ProductScanLogEvent
            {
                Timestamp = start.AddDays(i),
                ProductNumber = "A",
                ProductName = "Test"
            });
        }

        await _db.SaveChangesAsync();

        var result = await _service.GetScanActivityAsync("month");

        Assert.That(result.PeriodType, Is.EqualTo("month"));
        Assert.That(result.Trend.Count, Is.GreaterThan(5));

        var last = result.Trend.Last();
        Assert.That(last.RollingAverage, Is.GreaterThan(0));
    }
    
    // ---------------------- LOW STOCK PRODUCTS - Returns empty list on API failure ----------------------
    
    [Test]
    public async Task GetLowStockProducts_ApiFails_ReturnsEmptyList()
    {
        _authMock.Setup(x => x.GetAccessTokenAsync())
            .ReturnsAsync("token");

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var result = await _service.GetLowStockProductsAsync();

        Assert.That(result, Is.Empty);
    }
    
    // ---------------------- PRODUCT GROUP DISTRIBUTION - Groups small categories into "Other" ---------------------
    
    [Test]
    public async Task ProductGroupDistribution_GroupsSmallCategoriesIntoOther()
    {
        for (int i = 1; i <= 10; i++)
        {
            _db.ProductScanLogs.Add(new ProductScanLogEvent
            {
                ProductGroup = "Group" + i,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        var result = await _service.GetProductGroupDistributionAsync();

        // Should return 6 items: 5 main groups + "Other"
        Assert.That(result.Count, Is.EqualTo(6));
        Assert.That(result.Any(x => x.ProductGroup == "Other"), Is.True);
    }
    
    // ---------------------- SCAN PERFORMANCE - Detects increasing error rate ---------------------
    
    [Test]
    public async Task GetProductsWithIncreasingErrorRate_DetectsTrends()
    {
        var now = DateTimeOffset.UtcNow;

        // Previous week
        _db.ProductScanLogs.AddRange(
            Enumerable.Range(0, 10).Select(_ => new ProductScanLogEvent
            {
                Timestamp = now.AddDays(-10),
                ProductNumber = "A",
                ProductName = "Test"
            })
        );

        _db.ProductScanReportLogs.Add(new ProductScanReportLogEvent
        {
            ProductNumber = "A",
            ProductName = "Test",
            CreatedAt = now.AddDays(-10)
        });

        // Current week
        _db.ProductScanLogs.AddRange(
            Enumerable.Range(0, 10).Select(_ => new ProductScanLogEvent
            {
                Timestamp = now.AddDays(-1),
                ProductNumber = "A",
                ProductName = "Test"
            })
        );

        _db.ProductScanReportLogs.AddRange(
            Enumerable.Range(0, 5).Select(_ => new ProductScanReportLogEvent
            {
                ProductNumber = "A",
                ProductName = "Test",
                CreatedAt = now.AddDays(-1)
            })
        );

        await _db.SaveChangesAsync();

        var result = await _service.GetProductsWithIncreasingErrorRateAsync();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].TrendChange, Is.GreaterThan(0));
    }
}
