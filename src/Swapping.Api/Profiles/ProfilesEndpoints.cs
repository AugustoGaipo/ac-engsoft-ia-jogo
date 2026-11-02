using System.Security.Claims;
using Microsoft.Extensions.Options;
using Swapping.Api.Hosting;

namespace Swapping.Api.Profiles;

/// <summary>The HTTP routes of <c>profiles</c>. They only translate service results to HTTP.</summary>
public static class ProfilesEndpoints
{
    public static IEndpointRouteBuilder MapProfiles(this IEndpointRouteBuilder app)
    {
        app.MapPost("/profiles", async (CreateProfileRequest body, ProfileService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(body.Name, body.Icon, ct);
            if (result.Value is not { } created)
                return Problem(result.Error!.Value);

            var profile = created.Profile;
            return Results.Created("/profiles/me", new CreateProfileResponse(
                profile.Id, profile.Name, profile.Icon, created.AccessCode, created.Token, created.TokenExpiresAt));
        }).AddEndpointFilter(NoStore).RequireRateLimiting(RateLimits.CreateProfile);

        app.MapPost("/sessions", async (LoginRequest body, ProfileService service, HttpContext http, CancellationToken ct) =>
        {
            var result = await service.LoginAsync(body.Name, body.AccessCode, ct);
            switch (result.Outcome)
            {
                case LoginOutcome.Ok:
                    var profile = result.Profile!;
                    return Results.Ok(new LoginResponse(profile.Id, profile.Name, profile.Icon, result.Token!, result.TokenExpiresAt));
                case LoginOutcome.Locked:
                    http.Response.Headers.RetryAfter = RetryAfterSeconds(result.RetryAfter);
                    return ApiProblem.Result(StatusCodes.Status423Locked, "ProfileLocked");
                default:
                    return ApiProblem.Result(StatusCodes.Status401Unauthorized, "InvalidCredentials");
            }
        }).AddEndpointFilter(NoStore).RequireRateLimiting(RateLimits.Login);

        app.MapPost("/profiles/me/access-code", async (RegenerateCodeRequest body, ClaimsPrincipal user, ProfileService service, HttpContext http, CancellationToken ct) =>
        {
            var result = await service.RegenerateAsync(user.ProfileId(), user.SessionId(), body.AccessCode, ct);
            if (result.NewCode is { } code)
                return Results.Ok(new RegenerateCodeResponse(code));
            if (result.Refusal!.Outcome == LoginOutcome.Locked)
            {
                http.Response.Headers.RetryAfter = RetryAfterSeconds(result.Refusal.RetryAfter);
                return ApiProblem.Result(StatusCodes.Status423Locked, "ProfileLocked");
            }
            return ApiProblem.Result(StatusCodes.Status401Unauthorized, "InvalidCredentials");
        }).RequireAuthorization().AddEndpointFilter(NoStore);

        app.MapGet("/icons", (IOptions<ProfilesOptions> options) => Results.Ok(options.Value.IconList));

        app.MapGet("/profiles/me", async (ClaimsPrincipal user, ProfileService service, CancellationToken ct) =>
            await service.GetAsync(user.ProfileId(), ct) is { } profile
                ? Results.Ok(ProfileResponse.From(profile))
                : ApiProblem.Result(StatusCodes.Status401Unauthorized, "InvalidToken"))
            .RequireAuthorization();

        app.MapPatch("/profiles/me", async (UpdateProfileRequest body, ClaimsPrincipal user, ProfileService service, CancellationToken ct) =>
            await service.ChangeIconAsync(user.ProfileId(), body.Icon, ct) is { } profile
                ? Results.Ok(ProfileResponse.From(profile))
                : Problem(ProfileError.UnknownIcon))
            .RequireAuthorization();

        app.MapDelete("/sessions/current", async (ClaimsPrincipal user, ProfileService service, CancellationToken ct) =>
        {
            await service.LogoutAsync(user.SessionId(), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }

    /// <summary>Answers that carry an access code or a token must never be cached (S7).</summary>
    private static async ValueTask<object?> NoStore(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        return await next(context);
    }

    /// <summary>Whole seconds, rounded up, never 0 (a client waiting 0 s would retry at once).</summary>
    public static string RetryAfterSeconds(TimeSpan wait) =>
        Math.Max(1, (long)Math.Ceiling(wait.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static IResult Problem(ProfileError error) => error switch
    {
        ProfileError.InvalidName => ApiProblem.Result(StatusCodes.Status400BadRequest, "InvalidName"),
        ProfileError.UnknownIcon => ApiProblem.Result(StatusCodes.Status400BadRequest, "UnknownIcon"),
        ProfileError.NameTaken => ApiProblem.Result(StatusCodes.Status409Conflict, "NameTaken"),
        _ => throw new ArgumentOutOfRangeException(nameof(error)),
    };
}

/// <summary>Body of <c>POST /profiles</c>. Both fields are required; unknown fields are refused.</summary>
public sealed class CreateProfileRequest
{
    public required string? Name { get; init; }

    public required string? Icon { get; init; }
}

/// <summary>Body of <c>POST /sessions</c>.</summary>
public sealed class LoginRequest
{
    public required string? Name { get; init; }

    public required string? AccessCode { get; init; }
}

public sealed record LoginResponse(Guid Id, string Name, string Icon, string Token, DateTimeOffset TokenExpiresAt);

/// <summary>Body of <c>POST /profiles/me/access-code</c>: the current code (optional; missing = wrong).</summary>
public sealed class RegenerateCodeRequest
{
    public string? AccessCode { get; init; }
}

public sealed record RegenerateCodeResponse(string AccessCode);

/// <summary>Body of <c>PATCH /profiles/me</c>: only the icon can change (A10).</summary>
public sealed class UpdateProfileRequest
{
    public required string? Icon { get; init; }
}

public sealed record ProfileResponse(Guid Id, string Name, string Icon, DateTimeOffset CreatedAt)
{
    public static ProfileResponse From(ProfileEntity profile) => new(profile.Id, profile.Name, profile.Icon, profile.CreatedAt);
}

public sealed record CreateProfileResponse(
    Guid Id, string Name, string Icon, string AccessCode, string Token, DateTimeOffset TokenExpiresAt);
