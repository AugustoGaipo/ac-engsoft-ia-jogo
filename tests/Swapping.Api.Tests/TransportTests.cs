using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Swapping.Api.Tests;

public class TransportTests
{
    [Fact]
    public async Task In_production_plain_http_is_redirected_to_https()
    {
        using var factory = new ApiFactory { EnvironmentName = "Production" };
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://api.example"),
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/icons");

        Assert.True(response.StatusCode is HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect or HttpStatusCode.Redirect);
        Assert.Equal("https", response.Headers.Location?.Scheme);
    }

    [Fact]
    public async Task In_production_https_answers_carry_HSTS()
    {
        using var factory = new ApiFactory { EnvironmentName = "Production" };
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://api.example") });

        var response = await client.GetAsync("/icons");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task In_development_http_is_served_without_redirect()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/icons");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_web_page_from_another_origin_gets_no_CORS_permission()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var get = new HttpRequestMessage(HttpMethod.Get, "/icons");
        get.Headers.Add("Origin", "https://evil.example");
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/sessions");
        preflight.Headers.Add("Origin", "https://evil.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");

        var getResponse = await client.SendAsync(get);
        var preflightResponse = await client.SendAsync(preflight);

        Assert.False(getResponse.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(preflightResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
