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
            /*Vi har to sikkerhedsniveauer
             1. user type
             2. role
            */
            
            // Både employees og customers
            options.AddPolicy("EmployeeOrCustomer", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("userType", "employee") ||
                    ctx.User.HasClaim("userType", "customer")));

            // Kun employees (Ingen rolle krav)
            options.AddPolicy("EmployeeOnly", policy =>
                policy.RequireClaim("userType", "employee"));

            // Kun Customers (Ingen rolle krav)
            options.AddPolicy("CustomerOnly", policy =>
                policy.RequireClaim("userType", "customer"));

            // Kun Admin
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("userType", "employee") &&
                    ctx.User.HasClaim("role", "Admin")));

            // Kun Basic
            options.AddPolicy("BasicOnly", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("userType", "employee") &&
                    ctx.User.HasClaim("role", "Basic")));
        });
    }
}
