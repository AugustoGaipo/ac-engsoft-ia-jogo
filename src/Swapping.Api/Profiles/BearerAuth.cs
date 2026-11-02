using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swapping.Api.Data;
using Swapping.Api.Hosting;

namespace Swapping.Api.Profiles;

/// <summary>
/// <c>Authorization: Bearer &lt;token&gt;</c> → the profile id of a live session (not expired, not
/// revoked). Only the SHA-256 of the token is looked up.
/// </summary>
public sealed class BearerAuth(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    GameDbContext db, TimeProvider time)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Bearer";
    public const string ProfileIdClaim = "profile_id";
    public const string SessionIdClaim = "session_id";

    private const string Prefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (header is null || !header.StartsWith(Prefix, StringComparison.Ordinal))
            return AuthenticateResult.NoResult();

        var token = header[Prefix.Length..].Trim();
        if (token.Length is 0 or > 100)
            return AuthenticateResult.Fail("Invalid token.");

        var hash = SessionEntity.HashToken(token);
        var now = time.GetUtcNow();
        var session = await db.Sessions.AsNoTracking()
            .SingleOrDefaultAsync(s => s.TokenHash == hash && s.ExpiresAt > now, Context.RequestAborted);
        if (session is null)
            return AuthenticateResult.Fail("Invalid token.");

        var identity = new ClaimsIdentity(
            [new Claim(ProfileIdClaim, session.ProfileId.ToString()), new Claim(SessionIdClaim, session.Id.ToString())],
            SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        ApiProblem.WriteAsync(Context, StatusCodes.Status401Unauthorized, "InvalidToken");
}

/// <summary>Reads the authenticated profile on a request (used by <c>profiles</c> and <c>progress</c>).</summary>
public static class CurrentProfile
{
    public static Guid ProfileId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(BearerAuth.ProfileIdClaim) ?? throw new InvalidOperationException("Not authenticated."));

    public static Guid SessionId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(BearerAuth.SessionIdClaim) ?? throw new InvalidOperationException("Not authenticated."));
}
