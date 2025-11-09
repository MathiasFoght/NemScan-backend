using NemScan_API.SwaggerAuth;
using NemScan_API.RateLimit;
using NemScan_API.Utils;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

builder.LoadEnvironmentVariables();

builder.Services.RegisterAppServices(builder.Configuration);

builder.Services.AddJwtAuth(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerAuth();
builder.Services.AddHealthChecks();
builder.Services.AddGlobalRateLimiting();


if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:5117");
}

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapHealthChecks("/health");
app.MapGet("/", () => "NemScan API is running");
app.MapControllers();
app.Run();
