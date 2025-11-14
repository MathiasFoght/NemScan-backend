using Microsoft.AspNetCore.Mvc;
using Moq;
using NemScan_API.Controllers;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO.Product;
using NemScan_API.Models.DTO.Statistics;
using NUnit.Framework;

namespace NemScan_API.Tests.Controllers;

[TestFixture]
public class StatisticsControllerTests
{
    private Mock<IStatisticsService> _statsMock = null!;
    private StatisticsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _statsMock = new Mock<IStatisticsService>();
        _controller = new StatisticsController(_statsMock.Object);
    }

    // ----------------------------
    // Scan Performance
    // ----------------------------

    [Test]
    public async Task ScanPerformance_ReturnsNotFound_WhenNoScans()
    {
        _statsMock.Setup(x => x.GetScanPerformanceAsync(null, null))
            .ReturnsAsync(new ScanPerformanceDTO { TotalScans = 0 });

        var response = await _controller.GetScanPerformance(null, null);

        Assert.That(response, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task ScanPerformance_ReturnsOk_WhenDataExists()
    {
        _statsMock.Setup(x => x.GetScanPerformanceAsync(null, null))
            .ReturnsAsync(new ScanPerformanceDTO { TotalScans = 10 });

        var result = await _controller.GetScanPerformance(null, null);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
    }

    // ----------------------------
    // Product Group Distribution
    // ----------------------------

    [Test]
    public async Task ProductGroupDistribution_ReturnsNotFound_WhenEmpty()
    {
        _statsMock.Setup(x => x.GetProductGroupDistributionAsync(null, null))
            .ReturnsAsync(new List<ProductGroupDistributionDTO>());

        var response = await _controller.GetProductGroupDistribution(null, null);

        Assert.That(response, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task ProductGroupDistribution_ReturnsOk_WhenDataExists()
    {
        _statsMock.Setup(x => x.GetProductGroupDistributionAsync(null, null))
            .ReturnsAsync(new List<ProductGroupDistributionDTO>
            {
                new() { ProductGroup = "Food", ScanCount = 5 }
            });

        var result = await _controller.GetProductGroupDistribution(null, null);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
    }

    // ----------------------------
    // Increasing Error Rate
    // ----------------------------

    [Test]
    public async Task IncreasingErrorRate_ReturnsNotFound_WhenEmpty()
    {
        _statsMock.Setup(x => x.GetProductsWithIncreasingErrorRateAsync(7))
            .ReturnsAsync(new List<ErrorRateTrendDTO>());

        var response = await _controller.GetIncreasingErrorRate(7);

        Assert.That(response, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task IncreasingErrorRate_ReturnsOk_WhenDataExists()
    {
        _statsMock.Setup(x => x.GetProductsWithIncreasingErrorRateAsync(7))
            .ReturnsAsync(new List<ErrorRateTrendDTO>
            {
                new() { ProductNumber = "1001", TrendChange = 50 }
            });

        var response = await _controller.GetIncreasingErrorRate(7);

        Assert.That(response, Is.TypeOf<OkObjectResult>());
    }

    // ----------------------------
    // Top Product Today
    // ----------------------------

    [Test]
    public async Task TopScannedProduct_ReturnsFallback_WhenNull()
    {
        _statsMock.Setup(x => x.GetMostScannedProductAsync())
            .ReturnsAsync((TopScannedProductDTO?)null);

        var response = await _controller.GetTopScannedProduct() as OkObjectResult;

        Assert.That(response, Is.Not.Null);

        var dict = response!.Value!
            .GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(response.Value));

        Assert.That(dict["productName"], Is.EqualTo("No scans today"));
        Assert.That(dict["productNumber"], Is.Null);
        Assert.That(dict["scanCount"], Is.EqualTo(0));
    }



    [Test]
    public async Task TopScannedProduct_ReturnsOk_WhenDataExists()
    {
        _statsMock.Setup(x => x.GetMostScannedProductAsync())
            .ReturnsAsync(new TopScannedProductDTO
            {
                ProductName = "Milk",
                ProductNumber = "1001",
                ScanCount = 20
            });

        var result = await _controller.GetTopScannedProduct();

        Assert.That(result, Is.TypeOf<OkObjectResult>());
    }

    // ----------------------------
    // Low Stock Products
    // ----------------------------

    [Test]
    public async Task LowStock_ReturnsNotFound_WhenEmpty()
    {
        _statsMock.Setup(x => x.GetLowStockProductsAsync(100))
            .ReturnsAsync(new List<LowStockProductDTO>());

        var response = await _controller.GetLowStockProducts(100);

        Assert.That(response, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task LowStock_ReturnsOk_WhenDataExists()
    {
        _statsMock.Setup(x => x.GetLowStockProductsAsync(100))
            .ReturnsAsync(new List<LowStockProductDTO>
            {
                new() { ProductNumber = "2001", CurrentStockQuantity = 5 }
            });

        var response = await _controller.GetLowStockProducts(100);

        Assert.That(response, Is.TypeOf<OkObjectResult>());
    }

    // ----------------------------
    // Scan Activity
    // ----------------------------

    [Test]
    public async Task ScanActivity_ReturnsOk_Always()
    {
        _statsMock.Setup(x => x.GetScanActivityAsync("week"))
            .ReturnsAsync(new WeeklyScanTrendResponse { PeriodType = "week" });

        var response = await _controller.GetScanActivity("week");

        Assert.That(response, Is.TypeOf<OkObjectResult>());
    }
}
