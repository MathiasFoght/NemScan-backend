using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NemScan_API.Config;
using NemScan_API.Interfaces;
using NemScan_API.Models;

namespace NemScan_API.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateEmployeeToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("employeeNumber", user.EmployeeNumber),
            new Claim("name", user.Name),
            new Claim("role", user.Role.ToString()),
            new Claim("userType", "employee")
        };
        
        return BuildJwt(claims);
    }

    public string GenerateCustomerToken(Customer customer)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new Claim("userType", "customer"),
            new Claim("scope", "barcode:read"),
        };
        
        if (!string.IsNullOrWhiteSpace(customer.DeviceId))
            claims.Add(new Claim("deviceId", customer.DeviceId));

        return BuildJwt(claims);
    }
    
    private string BuildJwt(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.Expire),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}