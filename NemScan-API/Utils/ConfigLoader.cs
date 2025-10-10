namespace NemScan_API.Utils;

public static class ConfigLoader
{
    public static void LoadEnvironmentVariables(this WebApplicationBuilder builder)
    {
        DotNetEnv.Env.Load();

        builder.Configuration["Jwt:Key"] = Environment.GetEnvironmentVariable("JWT_KEY");
        builder.Configuration["Jwt:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER");
        builder.Configuration["Jwt:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        builder.Configuration["Jwt:Expire"] = Environment.GetEnvironmentVariable("JWT_EXPIRE");

        builder.Configuration["AmeroAuth:AuthUrl"] = Environment.GetEnvironmentVariable("AMERO_AUTH_URL");
        builder.Configuration["AmeroAuth:ClientId"] = Environment.GetEnvironmentVariable("AMERO_CLIENT_ID");
        builder.Configuration["AmeroAuth:ApiKey"] = Environment.GetEnvironmentVariable("AMERO_API_KEY");
        builder.Configuration["AmeroAuth:Audience"] = Environment.GetEnvironmentVariable("AMERO_AUDIENCE");
        builder.Configuration["AmeroAuth:Scope"] = Environment.GetEnvironmentVariable("AMERO_SCOPE");

        builder.Configuration["AZURE_POSTGRES_CONNECTION"] = Environment.GetEnvironmentVariable("AZURE_POSTGRES_CONNECTION");
        
        builder.Configuration["AZURE_STORAGE_CONNECTION"] = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION");
        builder.Configuration["AZURE_STORAGE_CONTAINER_NAME"] = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONTAINER_NAME");
    }

}