using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NemScan_API.Interfaces;
using NemScan_API.Models;
using NemScan_API.Models.Auth;
using NemScan_API.Models.DTO;
using AuthLogEvent = NemScan_API.Models.Events.AuthLogEvent;
using IAuthService = NemScan_API.Interfaces.IAuthService;

namespace NemScan_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogEventPublisher _logEventPublisher;
    public record CustomerTokenRequest(string DeviceId);
    public record LoginRequest(string EmployeeNumber);

    public AuthController(IAuthService authService, IJwtTokenService jwtTokenService, ILogEventPublisher logEventPublisher)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
        _logEventPublisher = logEventPublisher;
    }
    
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmployeeNumber))
            return BadRequest("Employee number is required");

        var user = await _authService.AuthenticateEmployeeAsync(request.EmployeeNumber);
        if (user == null)
        {
            await _logEventPublisher.PublishAsync(new AuthLogEvent
            {
                EventType = "auth.login.failed",
                EmployeeNumber = request.EmployeeNumber,
                Success = false,
                Message = "Invalid employee number entered"
            }, "auth.login.failed");
            
            return Unauthorized();
        }

        if (!user.IsValidPosition())
            return BadRequest("Invalid role/position combination.");
        
        var token = _jwtTokenService.GenerateEmployeeToken(user);
        
        await _logEventPublisher.PublishAsync(new AuthLogEvent
        {
            EventType = "auth.login.success",
            EmployeeNumber = request.EmployeeNumber,
            Success = true,
            Message = $"Employee ({user.Name}) login successful"
        }, "auth.login.success");

        var userDto = new EmployeeLoginDTO
        {
            EmployeeNumber = user.EmployeeNumber,
            Name = user.Name,
            Role = user.Role.ToString(),
            Position = user.Position.ToString(),
            StoreNumber = user.StoreNumber,
        };

        return Ok(new
        {
            Employee = userDto,
            Token = token,
        });
    }
    
    [HttpPost("customerToken")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCustomerToken([FromBody] CustomerTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            await _logEventPublisher.PublishAsync(new AuthLogEvent
            {
                EventType = "auth.customer.token",
                DeviceId = request.DeviceId,
                Success = false,
                Message = "Customer token generation failed. Device id is required"
            }, "auth.customer.token");
            
            return BadRequest("Device id is required");
        }
        
        var customer = new Customer
        {
            DeviceId = request.DeviceId,
        };

        var token = _jwtTokenService.GenerateCustomerToken(customer);
        
        await _logEventPublisher.PublishAsync(new AuthLogEvent
        {
            EventType = "auth.customer.token",
            DeviceId = request.DeviceId,
            Success = true,
            Message = "Customer token generated"
        }, "auth.customer.token");
        
        return Ok(new { Token = token });
    }
}