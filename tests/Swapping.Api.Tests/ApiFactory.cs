using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    public Dictionary<string, string?> Settings { get; } = new();

    public string EnvironmentName { get; init; } = "Development";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(Settings));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<GameDbContext>>();
            services.AddDbContext<GameDbContext>(options => options.UseSqlite(connection));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            connection.Dispose();
    }
}
