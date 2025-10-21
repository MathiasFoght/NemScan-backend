using Microsoft.EntityFrameworkCore;
using NemScan_API.Config;
using NemScan_API.Interfaces;
using NemScan_API.Logging.Consumers;
using NemScan_API.Logging.Publisher;
using NemScan_API.Services.Amero;
using NemScan_API.Services.Auth;
using NemScan_API.Services.Employee;
using NemScan_API.Services.Product;
using NemScan_API.Services.ProductCampaign;
using NemScan_API.Services.Report;
using NemScan_API.Services.Statistics;

namespace NemScan_API.Utils;

public static class ServiceRegistration
{
    public static void RegisterAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<AmeroAuthConfig>(configuration.GetSection("AmeroAuth"));
        
        var connectionString = configuration["AZURE_POSTGRES_CONNECTION"];
        services.AddDbContext<NemScanDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IAuthService, AuthService>();
        
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddHttpClient<IAmeroAuthService, AmeroAuthService>();

        services.AddScoped<IProductCustomerService, ProductCustomerService>();
        
        services.AddScoped<IProductEmployeeService, ProductEmployeeService>();
        
        services.AddHttpClient<IProductImageService, ProductImageService>();
        
        services.AddSingleton<IEmployeeService, EmployeeProfileService>();
        
        services.AddScoped<IStatisticsService, StatisticsService>();
        
        services.AddScoped<IProductCampaignService, ProductCampaignService>();
        
        services.AddScoped<IReportService, ReportService>();


        
        services.Configure<RabbitMqConfig>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<ILogEventPublisher, RabbitMqPublisher>();
        
        services.AddHostedService<AuthLogConsumer>();
        services.AddHostedService<EmployeeLogConsumer>();
        services.AddHostedService<ProductLogConsumer>();
    }
}