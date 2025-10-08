using Microsoft.EntityFrameworkCore;
using NemScan_API.Config;
using NemScan_API.Interfaces;
using NemScan_API.Services;
using NemScan_API.Services.Product;

namespace NemScan_API.Utils;

public static class ServiceRegistration
{
    public static void RegisterAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<AmeroAuthConfig>(configuration.GetSection("AmeroAuth"));

        var connectionString = configuration["POSTGRES_CONNECTION"];
        services.AddDbContext<NemScanDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddHttpClient<IAmeroAuthService, AmeroAuthService>();

        services.AddScoped<IProductCustomerService, ProductCustomerService>();
        services.AddScoped<IProductEmployeeService, ProductEmployeeService>();
        services.AddHttpClient<IProductImageService, ProductImageService>();
    }
}