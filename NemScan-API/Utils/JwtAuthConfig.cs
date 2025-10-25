using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace NemScan_API.Utils;

public static class JwtAuthConfig
{
    public static void AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"];
        var jwtIssuer = configuration["Jwt:Issuer"];
        var jwtAudience = configuration["Jwt:Audience"];

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
                    RoleClaimType = "role",
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };
            });
        
        services.AddAuthorization(options =>
        {
            // Employees and customers
            options.AddPolicy("EmployeeOrCustomer", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("userType", "employee") ||
                    ctx.User.HasClaim("userType", "customer")));

            // Only employees (No role required)
            options.AddPolicy("EmployeeOnly", policy =>
                policy.RequireClaim("userType", "employee"));

            // Only Customers (No role required)
            options.AddPolicy("CustomerOnly", policy =>
                policy.RequireClaim("userType", "customer"));

            // Only Admin
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("userType", "employee") &&
                    ctx.User.HasClaim("role", "Admin")));

            // Only Basic
            options.AddPolicy("BasicOnly", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("userType", "employee") &&
                    ctx.User.HasClaim("role", "Basic")));
        });
    }
}
