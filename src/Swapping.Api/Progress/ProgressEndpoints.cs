using System.Security.Claims;
using Swapping.Api.Hosting;
using Swapping.Api.Profiles;

namespace Swapping.Api.Progress;

/// <summary>The HTTP routes of <c>progress</c>. They only translate service results to HTTP.</summary>
public static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgress(this IEndpointRouteBuilder app)
    {
        app.MapGet("/levels", (LevelDirectory levels) => Results.Ok(levels.Levels));

        app.MapPost("/results", async (SubmitResultRequest body, ClaimsPrincipal user, ProgressService service, CancellationToken ct) =>
        {
            if (body.LevelId is null)
                return ApiProblem.Result(StatusCodes.Status400BadRequest, "InvalidBody");

            var values = new ResultValues(body.TimeMs, body.Swaps, body.Deaths, body.RoomRestarts);
            var result = await service.SubmitAsync(user.ProfileId(), new SubmittedResult(body.LevelId, values, body.FinishedAt), ct);
            return result.Outcome is { } outcome
                ? Results.Ok(outcome)
                : Problem(result.Error!.Value);
        }).RequireAuthorization().RequireRateLimiting(RateLimits.Uploads);

        app.MapGet("/progress/me", async (ClaimsPrincipal user, ProgressService service, CancellationToken ct) =>
            Results.Ok(await service.GetProgressAsync(user.ProfileId(), ct)))
            .RequireAuthorization();

        // Public (B6). The level id is checked against the catalog before any query (P4).
        app.MapGet("/rankings/{levelId}", async (string levelId, HttpRequest request, LevelDirectory levels, ProgressService service, CancellationToken ct) =>
        {
            if (levels.Find(levelId) is not { } level)
                return UnknownLevel();
            if (!RankingQuery.TryMode(request.Query, out var mode) || !RankingQuery.TryTop(request.Query, out var top))
                return InvalidQuery();

            var entries = await service.GetRankingAsync(level.LevelId, mode, top, ct);
            return Results.Ok(new RankingView(level.LevelId, RankingQuery.Name(mode), entries));
        });

        app.MapGet("/rankings/{levelId}/me", async (string levelId, HttpRequest request, ClaimsPrincipal user, LevelDirectory levels, ProgressService service, CancellationToken ct) =>
        {
            if (levels.Find(levelId) is not { } level)
                return UnknownLevel();
            if (!RankingQuery.TryMode(request.Query, out var mode))
                return InvalidQuery();

            return await service.GetMyRankAsync(user.ProfileId(), level.LevelId, mode, ct) is { } mine
                ? Results.Ok(mine)
                : ApiProblem.Result(StatusCodes.Status404NotFound, "NoResult");
        }).RequireAuthorization();

        return app;
    }

    private static IResult UnknownLevel() => ApiProblem.Result(StatusCodes.Status404NotFound, "UnknownLevel");

    private static IResult InvalidQuery() => ApiProblem.Result(StatusCodes.Status400BadRequest, "InvalidQuery");

    private static IResult Problem(SubmitError error) => error switch
    {
        SubmitError.UnknownLevel => ApiProblem.Result(StatusCodes.Status404NotFound, "UnknownLevel"),
        SubmitError.LevelLocked => ApiProblem.Result(StatusCodes.Status409Conflict, "LevelLocked"),
        SubmitError.ImplausibleResult => ApiProblem.Result(StatusCodes.Status422UnprocessableEntity, "ImplausibleResult"),
        _ => throw new ArgumentOutOfRangeException(nameof(error)),
    };

    /// <summary>Registers the catalog, read from <c>Progress:LevelsDirectory</c> relative to the content root.</summary>
    public static IServiceCollection AddProgress(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProgressOptions>(configuration.GetSection(ProgressOptions.Section));
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProgressOptions>>().Value;
            var root = provider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
            return LevelDirectory.Load(Path.GetFullPath(Path.Combine(root, options.LevelsDirectory)));
        });
        services.AddScoped<ProgressService>();
        return services;
    }
}

/// <summary>
/// Body of <c>POST /results</c>. Every field is required and must be a JSON integer where an int is
/// expected (P5); there is no profile field (P2) and no ghost field (P10): unknown fields are refused.
/// </summary>
public sealed class SubmitResultRequest
{
    public required string? LevelId { get; init; }

    public required int TimeMs { get; init; }

    public required int Swaps { get; init; }

    public required int Deaths { get; init; }

    public required int RoomRestarts { get; init; }

    public required DateTimeOffset FinishedAt { get; init; }
}
