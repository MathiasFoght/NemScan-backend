using NemScan_API.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddScoped<AuthService>();
builder.Services.Configure<AmeroApiOptions>(builder.Configuration.GetSection("AmeroApi"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ProductService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/api/test/token", async (AuthService authService, IOptions<AmeroApiOptions> options) =>
{
    var token = await authService.GetAccessTokenAsync(
        options.Value.ClientId,
        options.Value.ClientSecret
    );

    if (token == null) return Results.BadRequest("Kunne ikke hente token.");
    return Results.Ok(new { token });
})
.WithName("GetToken")
.WithOpenApi();

app.MapGet("/api/product/{productUid}", async (
    string productUid,
    ProductService productService,
    IOptions<AmeroApiOptions> options) =>
{
    var product = await productService.GetProductAsync(
        productUid,
        options.Value.ClientId,
        options.Value.ClientSecret
    );

    if (product == null) return Results.NotFound("Produkt ikke fundet.");
    return Results.Ok(product);
})
.WithName("GetProduct")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}