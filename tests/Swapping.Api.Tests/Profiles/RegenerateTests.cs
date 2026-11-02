using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Swapping.Api.Data;
using Swapping.Api.Hosting;
using Swapping.Api.Profiles;

namespace Swapping.Api.Tests.Profiles;

public class RegenerateTests
{
    private const string WrongCode = "0000-0000-0000-0000";
    private static readonly Regex CodeFormat = new("^[0-9A-HJKMNP-TV-Z]{4}(-[0-9A-HJKMNP-TV-Z]{4}){3}$");

    private static Task<HttpResponseMessage> Login(HttpClient client, string name, string code) =>
        client.PostAsJsonAsync("/sessions", new { name, accessCode = code });

    private static Task<HttpResponseMessage> Regenerate(HttpClient client, string token, object body) =>
        client.SendWithTokenAsync(HttpMethod.Post, "/profiles/me/access-code", token, body);

    private static async Task<string> TokenOf(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("token").GetString()!;
    }

    private static int FailedAttempts(ApiFactory factory, Guid id)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<GameDbContext>().Profiles.Single(p => p.Id == id).FailedAttempts;
    }

    [Fact]
    public async Task A_new_code_replaces_the_old_one_and_revokes_the_other_sessions()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        var otherPcToken = await TokenOf(await Login(client, "Ana", ana.AccessCode));

        var response = await Regenerate(client, ana.Token, new { accessCode = ana.AccessCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var newCode = body.RootElement.GetProperty("accessCode").GetString()!;
        Assert.Matches(CodeFormat, newCode);
        Assert.NotEqual(ana.AccessCode, newCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "Ana", ana.AccessCode)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, "Ana", newCode)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", ana.Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", otherPcToken)).StatusCode);
    }

    [Fact]
    public async Task Regenerating_does_not_touch_other_profiles()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        var bob = await client.CreateProfileAsync("Bob");

        await Regenerate(client, ana.Token, new { accessCode = ana.AccessCode });

        Assert.Equal(HttpStatusCode.OK, (await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", bob.Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, "Bob", bob.AccessCode)).StatusCode);
    }

    [Fact]
    public async Task A_wrong_current_code_is_InvalidCredentials_and_counts_as_a_failure()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");

        var response = await Regenerate(client, ana.Token, new { accessCode = WrongCode });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("InvalidCredentials", await response.ProblemCodeAsync());
        Assert.Equal(1, FailedAttempts(factory, ana.Id));
        Assert.Equal(HttpStatusCode.OK, (await Login(client, "Ana", ana.AccessCode)).StatusCode); // code unchanged
    }

    [Fact]
    public async Task Without_the_current_code_it_is_InvalidCredentials_and_counts_as_a_failure()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");

        var response = await Regenerate(client, ana.Token, new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("InvalidCredentials", await response.ProblemCodeAsync());
        Assert.Equal(1, FailedAttempts(factory, ana.Id));
    }

    [Fact]
    public async Task A_stolen_token_cannot_guess_the_code_forever()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        for (var i = 0; i < 5; i++)
            await Regenerate(client, ana.Token, new { accessCode = WrongCode });

        var withRightCode = await Regenerate(client, ana.Token, new { accessCode = ana.AccessCode });

        Assert.Equal((HttpStatusCode)423, withRightCode.StatusCode);
        Assert.Equal("ProfileLocked", await withRightCode.ProblemCodeAsync());
        Assert.NotNull(withRightCode.Headers.RetryAfter);
    }

    [Fact]
    public async Task Without_a_token_it_is_InvalidToken()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");

        var response = await client.PostAsJsonAsync("/profiles/me/access-code", new { accessCode = ana.AccessCode });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("InvalidToken", await response.ProblemCodeAsync());
    }

    [Fact]
    public async Task Answers_that_carry_a_code_or_a_token_are_never_cached()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/profiles", new { name = "Ana", icon = "icon-01" });
        using var body = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var code = body.RootElement.GetProperty("accessCode").GetString()!;
        var token = body.RootElement.GetProperty("token").GetString()!;
        var login = await Login(client, "Ana", code);
        var regenerate = await Regenerate(client, token, new { accessCode = code });

        Assert.All(new[] { create, login, regenerate }, r => Assert.True(r.Headers.CacheControl?.NoStore));
    }

    [Fact]
    public async Task A_lockout_writes_one_security_event()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");

        for (var i = 0; i < 6; i++)
            await Login(client, "Ana", WrongCode);

        var lockouts = factory.SecurityEvents.Where(e => e.Values.GetValueOrDefault("Event") == SecurityEvent.LockoutStarted).ToList();
        var lockout = Assert.Single(lockouts);
        Assert.Equal(ana.Id.ToString(), lockout.Values["ProfileId"]);
        Assert.True(lockout.Values.ContainsKey("RemoteIp"));
    }

    [Fact]
    public async Task Refused_logins_and_code_regeneration_write_security_events()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");

        await Login(client, "Ana", WrongCode);
        await Login(client, "Nobody", WrongCode);
        await Regenerate(client, ana.Token, new { accessCode = ana.AccessCode });

        var events = factory.SecurityEvents.Select(e => e.Values.GetValueOrDefault("Event")).ToList();
        Assert.Equal(2, events.Count(e => e == SecurityEvent.LoginRefused));
        Assert.Single(events, e => e == SecurityEvent.AccessCodeRegenerated);
    }

    [Fact]
    public async Task No_log_line_ever_contains_a_code_or_a_token()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        var loginToken = await TokenOf(await Login(client, "Ana", ana.AccessCode));
        await Login(client, "Ana", WrongCode);
        var regenerate = await Regenerate(client, ana.Token, new { accessCode = ana.AccessCode });
        using var body = JsonDocument.Parse(await regenerate.Content.ReadAsStringAsync());
        var newCode = body.RootElement.GetProperty("accessCode").GetString()!;
        await client.SendWithTokenAsync(HttpMethod.Delete, "/sessions/current", ana.Token);

        var secrets = new[] { ana.AccessCode, newCode, WrongCode }
            .SelectMany(c => new[] { c, AccessCode.Normalize(c) })
            .Concat(new[] { ana.Token, loginToken })
            .ToList();
        var lines = factory.Logs.Lines.ToList();
        Assert.NotEmpty(lines);
        Assert.DoesNotContain(lines, line => secrets.Any(s => line.AllText.Contains(s, StringComparison.OrdinalIgnoreCase)));
    }
}
