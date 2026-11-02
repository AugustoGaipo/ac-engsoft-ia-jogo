using System.Net;
using System.Net.Http.Json;
using Swapping.Api.Hosting;

namespace Swapping.Api.Tests.Profiles;

/// <summary>The rate limits with the production numbers (the factory raises them for other tests).</summary>
public class RateLimitTests
{
    private static ApiFactory WithProductionLimits() => new()
    {
        Settings =
        {
            ["Profiles:RateLimits:CreateProfilePerHour"] = "5",
            ["Profiles:RateLimits:LoginPerMinute"] = "10",
            ["Profiles:RateLimits:UploadsPerMinute"] = "30",
            ["Profiles:RateLimits:DefaultPerMinute"] = "120",
        },
    };

    [Fact]
    public async Task The_6th_profile_creation_from_one_IP_within_an_hour_is_429()
    {
        using var factory = WithProductionLimits();
        var client = factory.CreateClient();
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/profiles", new { name = $"Player{i}", icon = "icon-01" })).StatusCode);

        var sixth = await client.PostAsJsonAsync("/profiles", new { name = "Player5", icon = "icon-01" });

        Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
        Assert.Equal("TooManyRequests", await sixth.ProblemCodeAsync());
        Assert.NotNull(sixth.Headers.RetryAfter?.Delta);
        Assert.True(sixth.Headers.RetryAfter!.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task The_11th_login_within_a_minute_is_429()
    {
        using var factory = WithProductionLimits();
        var client = factory.CreateClient();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/sessions", new { name = "Nobody", accessCode = "x" })).StatusCode);

        var eleventh = await client.PostAsJsonAsync("/sessions", new { name = "Nobody", accessCode = "x" });

        Assert.Equal(HttpStatusCode.TooManyRequests, eleventh.StatusCode);
        Assert.NotNull(eleventh.Headers.RetryAfter);
    }

    [Fact]
    public async Task A_fake_X_Forwarded_For_does_not_give_a_new_allowance()
    {
        using var factory = WithProductionLimits();
        var client = factory.CreateClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/sessions")
            {
                Content = JsonContent.Create(new { name = "Nobody", accessCode = "x" }),
            };
            request.Headers.Add("X-Forwarded-For", $"203.0.113.{i}");
            last = await client.SendAsync(request);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [Fact]
    public async Task Logins_and_creations_have_separate_allowances()
    {
        using var factory = WithProductionLimits();
        var client = factory.CreateClient();
        for (var i = 0; i < 10; i++)
            await client.PostAsJsonAsync("/sessions", new { name = "Nobody", accessCode = "x" });

        var create = await client.PostAsJsonAsync("/profiles", new { name = "Ana", icon = "icon-01" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    [Fact]
    public async Task Other_routes_use_the_default_limit()
    {
        using var factory = new ApiFactory { Settings = { ["Profiles:RateLimits:DefaultPerMinute"] = "3" } };
        var client = factory.CreateClient();
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/icons")).StatusCode);

        var fourth = await client.GetAsync("/icons");

        Assert.Equal(HttpStatusCode.TooManyRequests, fourth.StatusCode);
    }

    [Fact]
    public async Task A_rate_limit_hit_writes_a_security_event()
    {
        using var factory = new ApiFactory { Settings = { ["Profiles:RateLimits:DefaultPerMinute"] = "1" } };
        var client = factory.CreateClient();

        await client.GetAsync("/icons");
        await client.GetAsync("/icons");

        Assert.Contains(factory.SecurityEvents, e => e.Values.GetValueOrDefault("Event") == SecurityEvent.RateLimitHit);
    }
}
