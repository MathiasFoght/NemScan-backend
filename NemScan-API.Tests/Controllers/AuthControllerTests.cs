using Microsoft.AspNetCore.Mvc;
using Moq;
using NemScan_API.Controllers;
using NemScan_API.Interfaces;
using NemScan_API.Models.Auth;
using NemScan_API.Models.Events;
using NUnit.Framework;

namespace NemScan_API.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private Mock<IAuthService> _authMock = null!;
    private Mock<IJwtTokenService> _jwtMock = null!;
    private Mock<ILogEventPublisher> _logMock = null!;
    private AuthController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _authMock = new Mock<IAuthService>();
        _jwtMock = new Mock<IJwtTokenService>();
        _logMock = new Mock<ILogEventPublisher>();

        _controller = new AuthController(_authMock.Object, _jwtMock.Object, _logMock.Object);
    }

    // ----------------------------
    // LOGIN TESTS
    // ----------------------------

    [Test]
    public async Task Login_ReturnsBadRequest_WhenEmployeeNumberMissing()
    {
        var request = new AuthController.LoginRequest("");

        var response = await _controller.Login(request);

        Assert.That(response, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Login_ReturnsUnauthorized_WhenInvalidEmployee()
    {
        _authMock.Setup(x => x.AuthenticateEmployeeAsync("9999", "9999"))
                 .ReturnsAsync((Employee?)null);

        var response = await _controller.Login(new AuthController.LoginRequest("9999"));

        Assert.That(response, Is.TypeOf<UnauthorizedObjectResult>());

        _logMock.Verify(x => x.PublishAsync(
            It.Is<AuthLogEvent>(e => e.Success == false),
            "auth.login.failed"
        ), Times.Once);
    }

    [Test]
    public async Task Login_ReturnsOk_WhenValidEmployee()
    {
        var emp = new Employee
        {
            EmployeeNumber = "123456",
            Name = "John Doe",
            Role = EmployeeRole.Basic,
            Position = EmployeePosition.ServiceAssistant,
            StoreNumber = "123456"
        };

        _authMock.Setup(x => x.AuthenticateEmployeeAsync("123", "123"))
                 .ReturnsAsync(emp);

        _jwtMock.Setup(x => x.GenerateEmployeeToken(emp))
                .Returns("valid-token");

        var result = await _controller.Login(new AuthController.LoginRequest("123"))
            as OkObjectResult;

        Assert.That(result, Is.Not.Null);

        var value = result!.Value!;
        var token = value.GetType().GetProperty("Token")!.GetValue(value) as string;

        Assert.That(token, Is.EqualTo("valid-token"));
    }

    // ----------------------------
    // CUSTOMER TOKEN TESTS
    // ----------------------------

    [Test]
    public async Task CustomerToken_ReturnsBadRequest_WhenDeviceIdMissing()
    {
        var result = await _controller.GetCustomerToken(new AuthController.CustomerTokenRequest(""));

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CustomerToken_ReturnsOk_WithToken()
    {
        _jwtMock.Setup(x => x.GenerateCustomerToken(It.IsAny<Customer>()))
                .Returns("customer-token");

        var result = await _controller.GetCustomerToken(
                new AuthController.CustomerTokenRequest("DEVICE123"))
            as OkObjectResult;

        Assert.That(result, Is.Not.Null);

        var value = result!.Value!;
        var token = value.GetType().GetProperty("Token")!.GetValue(value) as string;

        Assert.That(token, Is.EqualTo("customer-token"));

        _logMock.Verify(x => x.PublishAsync(
            It.Is<AuthLogEvent>(e => e.Success == true),
            "auth.customer.token"
        ), Times.Once);
    }
}
