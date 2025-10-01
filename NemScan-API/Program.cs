using NemScan_API.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.LoadEnvironmentVariables();

builder.Services.RegisterAppServices(builder.Configuration);

builder.Services.AddJwtAuth(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    // For at kunne teste mobilapp lokalt på et fysisk device
    builder.WebHost.UseUrls("http://0.0.0.0:5117");
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();