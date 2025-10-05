using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NemScan_API.Interfaces;
using NemScan_API.Models;
using NemScan_API.Models.DTO;
using IAuthService = NemScan_API.Interfaces.IAuthService;

namespace NemScan_API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;
    public record CustomerTokenRequest(string? DeviceId);
    public record LoginRequest(string EmployeeNumber);

    public AuthController(IAuthService authService, IJwtTokenService jwtTokenService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }
    
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmployeeNumber))
            return BadRequest("Employee number is required.");

        var user = await _authService.AuthenticateEmployeeAsync(request.EmployeeNumber);
        if (user == null)
            return Unauthorized();
        
        var token = _jwtTokenService.GenerateEmployeeToken(user);

        var userDto = new UserDTO
        {
            EmployeeNumber = user.EmployeeNumber,
            Name = user.Name,
            Role = user.Role.ToString()
        };

        return Ok(new
        {
            User = userDto,
            Token = token,
        });
    }
    
    [HttpPost("customerToken")]
    [AllowAnonymous]
    public IActionResult GetCustomerToken([FromBody] CustomerTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest("Device id is required.");
        var customer = new Customer
        {
            DeviceId = request.DeviceId
        };

        var token = _jwtTokenService.GenerateCustomerToken(customer);
        return Ok(new { Token = token });
    }
}