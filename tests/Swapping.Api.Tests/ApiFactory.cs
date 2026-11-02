using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Swapping.Api.Data;

namespace Swapping.Api.Tests;

/// <summary>
/// The API in memory: SQLite in memory (one connection kept open for the test), a fake clock and
/// extra configuration per test.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    public FakeTime Clock { get; } = new(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// Extra configuration. The rate limits start very high so tests are not limited by accident;
    /// <c>RateLimitTests</c> set the production numbers.
    /// </summary>
    public Dictionary<string, string?> Settings { get; } = new()
    {
        ["Profiles:RateLimits:CreateProfilePerHour"] = "100000",
        ["Profiles:RateLimits:LoginPerMinute"] = "100000",
        ["Profiles:RateLimits:UploadsPerMinute"] = "100000",
        ["Profiles:RateLimits:DefaultPerMinute"] = "100000",
    };

    public string EnvironmentName { get; init; } = "Development";

    /// <summary>Replaces the clock with one that throws, to provoke a server error.</summary>
    public bool BreakClock { get; init; }

    /// <summary>
    /// A temporary database file instead of memory, for tests with concurrent requests (each request
    /// opens its own connection, as in production). The file is deleted on dispose.
    /// </summary>
    public bool UseFileDatabase { get; init; }

    private string? databaseFile;

    /// <summary>Every log line written by the API during the test.</summary>
    public TestLoggerProvider Logs { get; } = new();

    /// <summary>The structured security events (category <c>Swapping.Security</c>).</summary>
    public IReadOnlyList<LogLine> SecurityEvents => Logs.Lines.Where(l => l.Category == "Swapping.Security").ToList();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (UseFileDatabase)
            databaseFile = Path.Combine(Path.GetTempPath(), $"swapping-test-{Guid.NewGuid():N}.db");
        else
            connection.Open();

        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(Settings));
        builder.ConfigureLogging(logging => logging.AddProvider(Logs).SetMinimumLevel(LogLevel.Trace));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<GameDbContext>>();
            if (databaseFile is not null)
                services.AddDbContext<GameDbContext>(options => options.UseSqlite($"Data Source={databaseFile}"));
            else
                services.AddDbContext<GameDbContext>(options => options.UseSqlite(connection));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(BreakClock ? new BrokenClock() : Clock);
        });
    }

    private sealed class BrokenClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("The test clock is broken.");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        connection.Dispose();
        if (databaseFile is not null)
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databaseFile);
        }
    }
}
