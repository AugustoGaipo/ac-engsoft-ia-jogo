using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Swapping.Api.Data;

namespace Swapping.Api.Tests;

public class HostTests
{
    [Fact]
    public async Task Unknown_route_answers_404_as_problem_details_with_a_code()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/no-such-route");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("NotFound", body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Every_response_has_nosniff()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/no-such-route");

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
    }

    [Fact]
    public async Task A_body_over_4_KB_is_refused_with_InvalidBody()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var json = "{\"x\":\"" + new string('a', 4096) + "\"}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/no-such-route", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(text);
        Assert.Equal("InvalidBody", body.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain("   at ", text); // no stack trace
    }

    [Fact]
    public async Task A_body_over_4_KB_without_a_length_header_is_refused_too()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var content = new StreamContent(new MemoryStream(new byte[5000]));
        content.Headers.ContentLength = null;

        var response = await client.PostAsync("/no-such-route", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_body_of_exactly_4_KB_passes_the_size_check()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var content = new ByteArrayContent(new byte[4096]);

        var response = await client.PostAsync("/no-such-route", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Tests_use_the_in_memory_database_not_a_file()
    {
        using var factory = new ApiFactory();
        _ = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        Assert.Contains(":memory:", db.Database.GetDbConnection().ConnectionString);
    }

    [Fact]
    public void Development_launch_settings_listen_only_on_127_0_0_1()
    {
        var path = Path.Combine(RepoPaths.Root, "src", "Swapping.Api", "Properties", "launchSettings.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));

        var urls = doc.RootElement.GetProperty("profiles").EnumerateObject()
            .Select(p => p.Value.GetProperty("applicationUrl").GetString()!)
            .SelectMany(u => u.Split(';'))
            .ToList();

        Assert.NotEmpty(urls);
        Assert.All(urls, u => Assert.Equal("127.0.0.1", new Uri(u).Host));
        Assert.False(doc.RootElement.TryGetProperty("iisSettings", out _));
    }

    [Theory]
    [InlineData("src/Swapping.Api")]
    [InlineData("tests/Swapping.Api.Tests")]
    public void NuGet_lock_files_exist(string project)
    {
        Assert.True(File.Exists(Path.Combine(RepoPaths.Root, project, "packages.lock.json")));
    }
}
