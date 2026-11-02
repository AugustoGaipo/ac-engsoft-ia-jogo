namespace Swapping.Api.Progress;

/// <summary>The HTTP routes of <c>progress</c>. They only translate service results to HTTP.</summary>
public static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgress(this IEndpointRouteBuilder app)
    {
        app.MapGet("/levels", (LevelDirectory levels) => Results.Ok(levels.Levels));

        return app;
    }

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
        return services;
    }
}
