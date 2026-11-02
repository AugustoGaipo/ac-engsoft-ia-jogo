namespace Swapping.Api.Hosting;

/// <summary>Names of the security events (S14, P11).</summary>
public static class SecurityEvent
{
    public const string LoginRefused = "LoginRefused";
    public const string LockoutStarted = "LockoutStarted";
    public const string AccessCodeRegenerated = "AccessCodeRegenerated";
    public const string RateLimitHit = "RateLimitHit";
    public const string ResultRefused = "ResultRefused";
}

/// <summary>
/// The security event log (S14): logger category <c>Swapping.Security</c>, one line per event with the
/// event name, the profile id (when known) and the remote IP. Never the code, the token or the body.
/// Kept in the server log only; nothing goes to the database (S16).
/// </summary>
public sealed class SecurityLog(ILoggerFactory loggers, IHttpContextAccessor http)
{
    public const string Category = "Swapping.Security";

    private readonly ILogger logger = loggers.CreateLogger(Category);

    public void Write(string securityEvent, Guid? profileId = null, string? detail = null)
    {
        var remoteIp = http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        logger.LogWarning("Security {Event} profile={ProfileId} ip={RemoteIp} detail={Detail}",
            securityEvent, profileId?.ToString() ?? "-", remoteIp, detail ?? "-");
    }
}
