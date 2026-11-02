using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
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
builder.Services.AddScoped<ProfileService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.EnsureCreated();

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    await next(context);
});
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<BodySizeLimit>();

app.MapProfiles();

app.Run();

/// <summary>Entry point; public so the tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
