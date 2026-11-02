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
        });

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
