using System.Net;
using System.Text.Json;

namespace Swapping.Api.Tests.Profiles;

public class SessionTests
{
    [Fact]
    public async Task The_token_from_creation_opens_my_profile()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana", "icon-04");

        var response = await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", ana.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(ana.Id, root.GetProperty("id").GetGuid());
        Assert.Equal("Ana", root.GetProperty("name").GetString());
        Assert.Equal("icon-04", root.GetProperty("icon").GetString());
        Assert.Equal(factory.Clock.GetUtcNow(), root.GetProperty("createdAt").GetDateTimeOffset());
        Assert.Equal(4, root.EnumerateObject().Count()); // nothing secret in the answer
    }

    [Fact]
    public async Task Each_token_sees_only_its_own_profile()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        var bob = await client.CreateProfileAsync("Bob");

        var response = await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", bob.Token);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(bob.Id, body.RootElement.GetProperty("id").GetGuid());
        Assert.NotEqual(ana.Id, bob.Id);
    }

    [Fact]
    public async Task No_token_is_InvalidToken()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/profiles/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("InvalidToken", await response.ProblemCodeAsync());
    }

    [Theory]
    [InlineData("Bearer not-a-real-token")]
    [InlineData("Bearer ")]
    [InlineData("Basic YW5hOjEyMw==")]
    [InlineData("bearer")]
    public async Task A_wrong_or_malformed_token_is_InvalidToken(string header)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await client.CreateProfileAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/profiles/me");
        request.Headers.TryAddWithoutValidation("Authorization", header);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("InvalidToken", await response.ProblemCodeAsync());
    }

    [Fact]
    public async Task The_access_code_is_not_a_token()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync();

        var response = await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", ana.AccessCode);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_works_until_one_second_before_30_days()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync();

        factory.Clock.Advance(TimeSpan.FromDays(30) - TimeSpan.FromSeconds(1));
        var response = await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", ana.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_token_stops_working_at_30_days()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync();

        factory.Clock.Advance(TimeSpan.FromDays(30));
        var response = await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", ana.Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("InvalidToken", await response.ProblemCodeAsync());
    }

    [Fact]
    public async Task Logout_answers_204_and_the_token_stops_working()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync();

        var logout = await client.SendWithTokenAsync(HttpMethod.Delete, "/sessions/current", ana.Token);
        var after = await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", ana.Token);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task Logout_without_a_token_is_InvalidToken()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/sessions/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Patch_changes_the_icon()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana", "icon-01");

        var patch = await client.SendWithTokenAsync(HttpMethod.Patch, "/profiles/me", ana.Token, new { icon = "icon-09" });
        var me = await client.SendWithTokenAsync(HttpMethod.Get, "/profiles/me", ana.Token);

        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        using var body = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal("icon-09", body.RootElement.GetProperty("icon").GetString());
    }

    [Fact]
    public async Task Patch_with_an_unknown_icon_is_UnknownIcon()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync();

        var response = await client.SendWithTokenAsync(HttpMethod.Patch, "/profiles/me", ana.Token, new { icon = "icon-99" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("UnknownIcon", await response.ProblemCodeAsync());
    }

    [Fact]
    public async Task Patch_cannot_change_the_name()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync();

        var response = await client.SendWithTokenAsync(HttpMethod.Patch, "/profiles/me", ana.Token, new { icon = "icon-02", name = "Eve" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidBody", await response.ProblemCodeAsync());
    }

    [Fact]
    public async Task Patch_without_a_token_is_InvalidToken()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PatchAsync("/profiles/me", System.Net.Http.Json.JsonContent.Create(new { icon = "icon-02" }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
