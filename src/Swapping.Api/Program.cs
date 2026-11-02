using Microsoft.EntityFrameworkCore;
using Swapping.Api.Data;
using Swapping.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblem.Customize);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Game") ?? "Data Source=swapping.db"));

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

app.Run();

/// <summary>Entry point; public so the tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
