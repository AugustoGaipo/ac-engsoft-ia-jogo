using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Swapping.Api.Profiles;

namespace Swapping.Api.Hosting;

/// <summary>Numbers of the rate limits (A6), section <c>Profiles:RateLimits</c>.</summary>
public sealed class RateLimitOptions
{
    public const string Section = "Profiles:RateLimits";

    public int CreateProfilePerHour { get; set; } = 5;

    public int LoginPerMinute { get; set; } = 10;

    public int UploadsPerMinute { get; set; } = 30;

    public int DefaultPerMinute { get; set; } = 120;
}

/// <summary>
/// The four rate-limit policies of the spec, fixed window, in memory (one server process). Clients
/// are told apart by the connection's remote IP only: <c>X-Forwarded-For</c> is never read (S12).
/// </summary>
public static class RateLimits
{
    public const string CreateProfile = "create-profile";
    public const string Login = "login";
    public const string Uploads = "uploads";
    public const string Default = "default";

    public static IServiceCollection AddGameRateLimits(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.Section));
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(CreateProfile, http => PerIp(http, o => o.CreateProfilePerHour, TimeSpan.FromHours(1)));
            limiter.AddPolicy(Login, http => PerIp(http, o => o.LoginPerMinute, TimeSpan.FromMinutes(1)));
            limiter.AddPolicy(Default, http => PerIp(http, o => o.DefaultPerMinute, TimeSpan.FromMinutes(1)));
            limiter.AddPolicy(Uploads, http => Window(
                "profile:" + (http.User.Identity?.IsAuthenticated == true ? http.User.ProfileId().ToString() : RemoteIp(http)),
                Read(http).UploadsPerMinute, TimeSpan.FromMinutes(1)));
            limiter.OnRejected = async (context, ct) =>
            {
                var http = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait))
                    http.Response.Headers.RetryAfter = Math.Max(1, (long)Math.Ceiling(wait.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                http.RequestServices.GetRequiredService<SecurityLog>().Write(SecurityEvent.RateLimitHit, detail: http.Request.Path);
                await ApiProblem.WriteAsync(http, StatusCodes.Status429TooManyRequests, "TooManyRequests");
            };
        });
        return services;
    }

    private static RateLimitPartition<string> PerIp(HttpContext http, Func<RateLimitOptions, int> limit, TimeSpan window) =>
        Window("ip:" + RemoteIp(http), limit(Read(http)), window);

    private static RateLimitPartition<string> Window(string key, int limit, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit,
            Window = window,
            QueueLimit = 0,
        });

    private static RateLimitOptions Read(HttpContext http) =>
        http.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;

    private static string RemoteIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
