using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Swapping.Api.Data;
using Swapping.Api.Hosting;
using Swapping.Api.Profiles;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblem.Customize);
builder.Services.Configure<JsonOptions>(options =>
{
    // Strict bodies (S10): unknown fields, numbers in strings and deep nesting are refused.
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.MaxDepth = 8;
});
// A body that cannot be read becomes an empty 400; the status code pages turn it into InvalidBody.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Game") ?? "Data Source=swapping.db"));
builder.Services.Configure<ProfilesOptions>(builder.Configuration.GetSection(ProfilesOptions.Section));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SecurityLog>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddAuthentication(BearerAuth.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, BearerAuth>(BearerAuth.SchemeName, null);
builder.Services.AddAuthorization();
builder.Services.AddGameRateLimits(builder.Configuration);
builder.Services.AddHttpsRedirection(options => options.HttpsPort = builder.Configuration.GetValue<int?>("HttpsPort") ?? 443);

// Forwarded headers are trusted only from proxies listed in configuration (none by default, S12).
var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var proxy in knownProxies)
        options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
});
// No CORS policy is registered on purpose (S11): browsers never get Access-Control-Allow-Origin.

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.EnsureCreated();

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    await next(context);
});
app.UseExceptionHandler();
if (knownProxies.Length > 0)
    app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    // Outside the presentation PC every request must be HTTPS (S6).
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePages();
app.UseMiddleware<BodySizeLimit>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGroup("").RequireRateLimiting(RateLimits.Default).MapProfiles();

app.Run();

/// <summary>Entry point; public so the tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
