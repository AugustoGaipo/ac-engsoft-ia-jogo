using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Swapping.Api.Data;

namespace Swapping.Api.Tests.Profiles;

public class CreateProfileTests
{
    private static readonly Regex CodeFormat = new("^[0-9A-HJKMNP-TV-Z]{4}(-[0-9A-HJKMNP-TV-Z]{4}){3}$");

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static int CountProfiles(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<GameDbContext>().Profiles.Count();
    }

    [Fact]
    public async Task Creating_a_profile_returns_201_with_a_code_and_a_token()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/profiles", new { name = "Ana", icon = "icon-03" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.True(Guid.TryParse(root.GetProperty("id").GetString(), out _));
        Assert.Equal("Ana", root.GetProperty("name").GetString());
        Assert.Equal("icon-03", root.GetProperty("icon").GetString());
        Assert.Matches(CodeFormat, root.GetProperty("accessCode").GetString()!);
        Assert.False(string.IsNullOrEmpty(root.GetProperty("token").GetString()));
        Assert.Equal(factory.Clock.GetUtcNow().AddDays(30), root.GetProperty("tokenExpiresAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task The_name_is_saved_trimmed()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/profiles", new { name = "  Ana  ", icon = "icon-01" });

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Ana", body.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task The_same_name_in_another_case_is_NameTaken()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/profiles", new { name = "Ana", icon = "icon-01" });

        var response = await client.PostAsJsonAsync("/profiles", new { name = "ANA", icon = "icon-02" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NameTaken", await CodeOf(response));
        Assert.Equal(1, CountProfiles(factory));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("Ana Maria")]
    [InlineData("Zoë")]
    [InlineData("[b]x[/b]")]
    [InlineData("Robert'); DROP TABLE Profiles;--")]
    public async Task An_invalid_name_is_InvalidName(string name)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/profiles", new { name, icon = "icon-01" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidName", await CodeOf(response));
        Assert.Equal(0, CountProfiles(factory)); // and the table still exists
    }

    [Fact]
    public async Task A_null_name_is_InvalidName()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/profiles", Json("""{"name":null,"icon":"icon-01"}"""));

        Assert.Equal("InvalidName", await CodeOf(response));
    }

    [Theory]
    [InlineData("icon-13")]
    [InlineData("ICON-01")]
    [InlineData("")]
    public async Task An_icon_not_in_the_list_is_UnknownIcon(string icon)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/profiles", new { name = "Ana", icon });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("UnknownIcon", await CodeOf(response));
    }

    [Fact]
    public async Task Get_icons_lists_the_12_icons()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var icons = await client.GetFromJsonAsync<string[]>("/icons");

        Assert.Equal(Enumerable.Range(1, 12).Select(i => $"icon-{i:00}"), icons);
    }

    [Theory]
    [InlineData("""{"name":"Ana","icon":"icon-01","id":"00000000-0000-0000-0000-000000000001"}""")]
    [InlineData("""{"name":"Ana","icon":"icon-01","accessCodeHash":"AAAA"}""")]
    [InlineData("""{"name":"Ana","icon":"icon-01","failedAttempts":0}""")]
    public async Task Unknown_fields_are_InvalidBody_and_nothing_is_created(string json)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/profiles", Json(json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidBody", await CodeOf(response));
        Assert.Equal(0, CountProfiles(factory));
    }

    [Theory]
    [InlineData("""{"name":"Ana","icon":""")]                       // malformed
    [InlineData("""{"name":"Ana"}""")]                              // missing field
    [InlineData("""{"name":123,"icon":"icon-01"}""")]               // number for a string
    [InlineData("""["Ana","icon-01"]""")]                           // wrong shape
    [InlineData("""{"name":{"a":{"b":{"c":{"d":{"e":{"f":{"g":{"h":{}}}}}}}}},"icon":"icon-01"}""")] // depth > 8
    [InlineData("")]                                                 // empty
    public async Task Malformed_bodies_are_InvalidBody(string json)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/profiles", Json(json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidBody", await CodeOf(response));
    }

    [Fact]
    public async Task A_body_that_is_not_json_is_InvalidBody()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/profiles", new StringContent("name=Ana", Encoding.UTF8, "text/plain"));

        Assert.Equal("InvalidBody", await CodeOf(response));
    }

    [Fact]
    public async Task The_database_keeps_only_SHA_256_hashes_of_the_code_and_the_token()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/profiles", new { name = "Ana", icon = "icon-01" });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var code = body.RootElement.GetProperty("accessCode").GetString()!;
        var token = body.RootElement.GetProperty("token").GetString()!;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var profile = db.Profiles.Single();
        var session = db.Sessions.Single();
        Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes(code.Replace("-", ""))), profile.AccessCodeHash);
        Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes(token)), session.TokenHash);
        Assert.Equal(profile.Id, session.ProfileId);

        var everyValue = DatabaseDump.AllValues(db);
        Assert.DoesNotContain(everyValue, v => v.Contains(code.Replace("-", "")) || v.Contains(code) || v.Contains(token));
    }

    [Fact]
    public async Task No_column_holds_an_IP_an_email_or_a_plain_secret()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/profiles", new { name = "Ana", icon = "icon-01" });

        using var scope = factory.Services.CreateScope();
        var columns = DatabaseDump.AllColumns(scope.ServiceProvider.GetRequiredService<GameDbContext>());

        Assert.NotEmpty(columns);
        Assert.DoesNotContain(columns, c => Regex.IsMatch(c, "ip|mail|address", RegexOptions.IgnoreCase));
        Assert.All(columns.Where(c => Regex.IsMatch(c, "code|token", RegexOptions.IgnoreCase)),
            c => Assert.EndsWith("Hash", c));
    }

    [Fact]
    public void The_schema_is_created_at_startup()
    {
        using var factory = new ApiFactory();
        _ = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var tables = DatabaseDump.Tables(scope.ServiceProvider.GetRequiredService<GameDbContext>());
        Assert.Contains("Profiles", tables);
        Assert.Contains("Sessions", tables);
    }

    [Fact]
    public async Task A_server_error_is_a_500_without_exception_details()
    {
        using var factory = new ApiFactory { BreakClock = true };
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/profiles", new { name = "Ana", icon = "icon-01" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Equal("ServerError", await CodeOf(response));
        Assert.DoesNotContain("Exception", text);
        Assert.DoesNotContain("   at ", text);
        Assert.DoesNotContain("clock", text, StringComparison.OrdinalIgnoreCase);
    }
}
