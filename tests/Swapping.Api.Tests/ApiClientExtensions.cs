using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Swapping.Api.Tests;

public sealed record NewProfile(Guid Id, string Name, string AccessCode, string Token);

public static class ApiClientExtensions
{
    public static async Task<NewProfile> CreateProfileAsync(this HttpClient client, string name = "Ana", string icon = "icon-01")
    {
        var response = await client.PostAsJsonAsync("/profiles", new { name, icon });
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        return new NewProfile(
            Guid.Parse(root.GetProperty("id").GetString()!),
            root.GetProperty("name").GetString()!,
            root.GetProperty("accessCode").GetString()!,
            root.GetProperty("token").GetString()!);
    }

    public static HttpRequestMessage WithToken(this HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public static Task<HttpResponseMessage> SendWithTokenAsync(this HttpClient client, HttpMethod method, string url, string token, object? json = null)
    {
        var request = new HttpRequestMessage(method, url).WithToken(token);
        if (json is not null)
            request.Content = JsonContent.Create(json);
        return client.SendAsync(request);
    }

    public static async Task<string?> ProblemCodeAsync(this HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(text))
            return null;
        using var body = JsonDocument.Parse(text);
        return body.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
