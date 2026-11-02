using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Swapping.Api.Data;

namespace Swapping.Api.Tests.Profiles;

public class LoginTests
{
    private const string WrongCode = "0000-0000-0000-0000";

    private static Task<HttpResponseMessage> Login(HttpClient client, string name, string code) =>
        client.PostAsJsonAsync("/sessions", new { name, accessCode = code });

    private static int FailedAttempts(ApiFactory factory, Guid id)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<GameDbContext>().Profiles.Single(p => p.Id == id).FailedAttempts;
    }

    [Fact]
    public async Task The_right_code_logs_in_with_a_new_token()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana", "icon-05");

        var response = await Login(client, "Ana", ana.AccessCode);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        var token = root.GetProperty("token").GetString()!;
        Assert.Equal(ana.Id, root.GetProperty("id").GetGuid());
        Assert.Equal("Ana", root.GetProperty("name").GetString());
        Assert.Equal("icon-05", root.GetProperty("icon").GetString());
        Assert.Equal(factory.Clock.GetUtcNow().AddDays(30), root.GetProperty("tokenExpiresAt").GetDateTimeOffset());
        Assert.NotEqual(ana.Token, token);
        Assert.Equal(HttpStatusCode.OK, (await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", token)).StatusCode);
    }

    [Fact]
    public async Task The_code_works_in_any_case_and_without_dashes_and_the_name_in_any_case()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");

        var response = await Login(client, " aNA ", ana.AccessCode.ToLowerInvariant().Replace("-", ""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_wrong_code_and_an_unknown_name_get_the_same_answer()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");

        var wrongCode = await Login(client, "Ana", WrongCode);
        var unknownName = await Login(client, "Nobody", ana.AccessCode);
        var invalidName = await Login(client, "a b", ana.AccessCode);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongCode.StatusCode);
        Assert.Equal("InvalidCredentials", await wrongCode.ProblemCodeAsync());
        var expected = await wrongCode.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, unknownName.StatusCode);
        Assert.Equal(expected, await unknownName.Content.ReadAsStringAsync());
        Assert.Equal(expected, await invalidName.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("""{"name":"Ana"}""")]
    [InlineData("""{"name":"Ana","accessCode":"x","extra":1}""")]
    public async Task A_malformed_login_body_is_InvalidBody(string json)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/sessions", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidBody", await response.ProblemCodeAsync());
    }

    [Fact]
    public async Task After_5_wrong_codes_even_the_right_code_is_ProfileLocked()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "Ana", WrongCode)).StatusCode);

        var response = await Login(client, "Ana", ana.AccessCode);

        Assert.Equal((HttpStatusCode)423, response.StatusCode);
        Assert.Equal("ProfileLocked", await response.ProblemCodeAsync());
        Assert.Equal(TimeSpan.FromMinutes(15), response.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task The_lock_still_holds_at_14_59_and_ends_at_15_minutes()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        for (var i = 0; i < 5; i++)
            await Login(client, "Ana", WrongCode);

        factory.Clock.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));
        var stillLocked = await Login(client, "Ana", ana.AccessCode);
        factory.Clock.Advance(TimeSpan.FromSeconds(1));
        var open = await Login(client, "Ana", ana.AccessCode);

        Assert.Equal((HttpStatusCode)423, stillLocked.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(1), stillLocked.Headers.RetryAfter?.Delta);
        Assert.Equal(HttpStatusCode.OK, open.StatusCode);
        Assert.Equal(0, FailedAttempts(factory, ana.Id));
    }

    [Fact]
    public async Task After_the_lock_ends_wrong_codes_count_from_zero_again()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        for (var i = 0; i < 5; i++)
            await Login(client, "Ana", WrongCode);
        factory.Clock.Advance(TimeSpan.FromMinutes(15));

        for (var i = 0; i < 4; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "Ana", WrongCode)).StatusCode);
        var fifth = await Login(client, "Ana", ana.AccessCode);

        Assert.Equal(HttpStatusCode.OK, fifth.StatusCode);
    }

    [Fact]
    public async Task A_good_login_resets_the_failure_counter()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        for (var i = 0; i < 4; i++)
            await Login(client, "Ana", WrongCode);

        Assert.Equal(HttpStatusCode.OK, (await Login(client, "Ana", ana.AccessCode)).StatusCode);
        for (var i = 0; i < 4; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "Ana", WrongCode)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Login(client, "Ana", ana.AccessCode)).StatusCode);
    }

    [Fact]
    public async Task A_token_made_before_the_lock_still_works_while_locked()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        for (var i = 0; i < 5; i++)
            await Login(client, "Ana", WrongCode);

        var response = await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", ana.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_codes_on_one_profile_do_not_lock_another()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await client.CreateProfileAsync("Ana");
        var bob = await client.CreateProfileAsync("Bob");
        for (var i = 0; i < 5; i++)
            await Login(client, "Ana", WrongCode);

        Assert.Equal(HttpStatusCode.OK, (await Login(client, "Bob", bob.AccessCode)).StatusCode);
    }

    [Fact]
    public async Task Twenty_racing_failure_counts_lock_after_exactly_5()
    {
        // The worst race: 20 wrong guesses that all passed the "is it locked?" check before any of
        // them was counted. Each one has its own DbContext (its own connection), as in production.
        using var factory = new ApiFactory { UseFileDatabase = true };
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        var now = factory.Clock.GetUtcNow();

        var scopes = Enumerable.Range(0, 20).Select(_ => factory.Services.CreateScope()).ToList();
        var results = await Task.WhenAll(scopes.Select(scope => Task.Run(() =>
            scope.ServiceProvider.GetRequiredService<Swapping.Api.Profiles.ProfileService>()
                .RegisterFailureAsync(ana.Id, now, CancellationToken.None))));
        scopes.ForEach(s => s.Dispose());

        Assert.Equal(5, results.Count(r => r.Outcome == Swapping.Api.Profiles.LoginOutcome.InvalidCredentials));
        Assert.Equal(15, results.Count(r => r.Outcome == Swapping.Api.Profiles.LoginOutcome.Locked));
        Assert.Equal(5, FailedAttempts(factory, ana.Id));
    }

    [Fact]
    public async Task Twenty_concurrent_wrong_logins_give_exactly_5_failures_and_15_locks()
    {
        using var factory = new ApiFactory
        {
            UseFileDatabase = true,
            Settings = { ["Profiles:RateLimits:LoginPerMinute"] = "1000" },
        };
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");

        var answers = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Login(client, "Ana", WrongCode)));

        Assert.Equal(5, answers.Count(a => a.StatusCode == HttpStatusCode.Unauthorized));
        Assert.Equal(15, answers.Count(a => a.StatusCode == (HttpStatusCode)423));
        Assert.Equal((HttpStatusCode)423, (await Login(client, "Ana", ana.AccessCode)).StatusCode);
    }
}
