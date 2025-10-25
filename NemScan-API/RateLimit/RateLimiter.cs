using System.Threading.RateLimiting;

namespace NemScan_API.RateLimit
{
    public static class RateLimiter
    {
        public static IServiceCollection AddGlobalRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // Global rate limiter for all requests based on jwt token
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    string key;

                    if (context.User.Identity?.IsAuthenticated == true)
                    {
                        key = context.User.FindFirst("employeeNumber")?.Value
                            ?? context.User.FindFirst("sub")?.Value
                            ?? "authenticated-unknown";
                    }
                    else
                    {
                        key = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                });

                options.RejectionStatusCode = 429;

                options.OnRejected = async (context, token) =>
                {
                    var response = context.HttpContext.Response;
                    response.ContentType = "application/json";
                    response.Headers["Retry-After"] = "60";

                    await response.WriteAsync(
                        "{\"error\":\"Too many requests. Please try again in 60 seconds.\"}",
                        token);
                };
            });

            return services;
        }
    }
}
