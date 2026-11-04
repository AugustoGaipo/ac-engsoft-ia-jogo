using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Swapping.Api.Data;
using Swapping.Api.Progress;

namespace Swapping.Api.Tests.Progress;

/// <summary>
/// <c>POST /results</c> and <c>GET /progress/me</c> with the test catalog level-1 (4 rooms),
/// level-2 (3 rooms), level-4 (2 rooms).
/// </summary>
public sealed class SubmitResultTests : IDisposable
{
    private readonly TestCatalog catalog = new();
    private readonly ApiFactory factory;
    private readonly HttpClient client;

    public SubmitResultTests()
    {
        factory = new ApiFactory { Settings = { ["Progress:LevelsDirectory"] = catalog.Directory } };
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        factory.Dispose();
        catalog.Dispose();
    }

    private static object Result(string levelId = "level-1", int timeMs = 60_000, int swaps = 12, int deaths = 0, int roomRestarts = 0) =>
        new { levelId, timeMs, swaps, deaths, roomRestarts, finishedAt = "2026-11-02T11:59:00Z" };

    private Task<HttpResponseMessage> Send(string token, object body) =>
        client.SendWithTokenAsync(HttpMethod.Post, "/results", token, body);

    private Task<HttpResponseMessage> SendRaw(string token, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/results")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }.WithToken(token);
        return client.SendAsync(request);
    }

    private static async Task<(bool ImprovedTime, bool ImprovedMoves, bool UnlockedNext)> Read(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(3, root.EnumerateObject().Count());
        return (root.GetProperty("improvedTime").GetBoolean(), root.GetProperty("improvedMoves").GetBoolean(),
            root.GetProperty("unlockedNext").GetBoolean());
    }

    private sealed record Best(int TimeMs, int Swaps);

    private sealed record LevelProgress(string LevelId, int Number, string Name, bool Unlocked, Best? BestTime, Best? BestMoves);

    private async Task<List<LevelProgress>> MyProgress(string token)
    {
        var response = await client.SendWithTokenAsync(HttpMethod.Get, "/progress/me", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.EnumerateArray().Select(l => new LevelProgress(
            l.GetProperty("levelId").GetString()!, l.GetProperty("number").GetInt32(), l.GetProperty("name").GetString()!,
            l.GetProperty("unlocked").GetBoolean(), ReadBest(l.GetProperty("bestTime")), ReadBest(l.GetProperty("bestMoves")))).ToList();
    }

    private static Best? ReadBest(JsonElement element) => element.ValueKind == JsonValueKind.Null
        ? null
        : new Best(element.GetProperty("timeMs").GetInt32(), element.GetProperty("swaps").GetInt32());

    private List<BestResultEntity> Rows()
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<GameDbContext>().BestResults.AsNoTracking()
            .OrderBy(b => b.LevelId).ThenBy(b => b.Mode).ToList();
    }

    // ---- G-SC 2, 3: unlocking follows the catalog ----

    [Fact]
    public async Task A_new_profile_has_only_the_first_level_unlocked_and_no_bests()
    {
        var ana = await client.CreateProfileAsync();

        var progress = await MyProgress(ana.Token);

        Assert.Equal(new[] { "level-1", "level-2", "level-4" }, progress.Select(p => p.LevelId));
        Assert.Equal(new[] { 1, 2, 4 }, progress.Select(p => p.Number));
        Assert.Equal("Test one", progress[0].Name);
        Assert.Equal(new[] { true, false, false }, progress.Select(p => p.Unlocked));
        Assert.All(progress, p => Assert.Null(p.BestTime));
        Assert.All(progress, p => Assert.Null(p.BestMoves));
    }

    [Fact]
    public async Task A_result_for_level_1_unlocks_level_2()
    {
        var ana = await client.CreateProfileAsync();

        var answer = await Read(await Send(ana.Token, Result("level-1")));

        Assert.Equal((true, true, true), answer);
        Assert.Equal(new[] { true, true, false }, (await MyProgress(ana.Token)).Select(p => p.Unlocked));
    }

    [Fact]
    public async Task A_result_for_level_2_unlocks_level_4_skipping_the_gap()
    {
        var ana = await client.CreateProfileAsync();
        await Read(await Send(ana.Token, Result("level-1")));

        var answer = await Read(await Send(ana.Token, Result("level-2")));

        Assert.True(answer.UnlockedNext);
        Assert.Equal(new[] { true, true, true }, (await MyProgress(ana.Token)).Select(p => p.Unlocked));
    }

    [Fact]
    public async Task The_last_level_unlocks_nothing()
    {
        var ana = await client.CreateProfileAsync();
        await Read(await Send(ana.Token, Result("level-1")));
        await Read(await Send(ana.Token, Result("level-2")));

        var answer = await Read(await Send(ana.Token, Result("level-4")));

        Assert.False(answer.UnlockedNext);
    }

    [Fact]
    public async Task UnlockedNext_is_true_only_on_the_upload_that_unlocked_the_next_level()
    {
        var ana = await client.CreateProfileAsync();
        await Read(await Send(ana.Token, Result("level-1", timeMs: 60_000, swaps: 12)));

        var faster = await Read(await Send(ana.Token, Result("level-1", timeMs: 50_000, swaps: 13)));

        Assert.Equal((true, false, false), faster);
    }

    // ---- G-SC 4: locked and unknown levels ----

    [Fact]
    public async Task A_result_for_a_locked_level_is_409_LevelLocked_and_nothing_is_stored()
    {
        var ana = await client.CreateProfileAsync();

        var response = await Send(ana.Token, Result("level-2"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("LevelLocked", await response.ProblemCodeAsync());
        Assert.Empty(Rows());
    }

    [Theory]
    [InlineData("level-3")]
    [InlineData("LEVEL-1")]
    [InlineData("../catalog")]
    [InlineData("")]
    public async Task A_result_for_a_level_not_in_the_catalog_is_404_UnknownLevel(string levelId)
    {
        var ana = await client.CreateProfileAsync();

        var response = await Send(ana.Token, Result(levelId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("UnknownLevel", await response.ProblemCodeAsync());
        Assert.Empty(Rows());
    }

    // ---- G-SC 5, 6: best per mode, idempotent re-send ----

    [Fact]
    public async Task The_best_time_and_the_best_moves_are_kept_separately()
    {
        var ana = await client.CreateProfileAsync();

        Assert.Equal((true, true, true), await Read(await Send(ana.Token, Result(timeMs: 60_000, swaps: 12))));
        Assert.Equal((true, false, false), await Read(await Send(ana.Token, Result(timeMs: 50_000, swaps: 20))));
        Assert.Equal((false, true, false), await Read(await Send(ana.Token, Result(timeMs: 70_000, swaps: 8))));

        var level1 = (await MyProgress(ana.Token))[0];
        Assert.Equal(new Best(50_000, 20), level1.BestTime);
        Assert.Equal(new Best(70_000, 8), level1.BestMoves);
    }

    [Fact]
    public async Task A_resent_result_that_is_not_better_is_200_and_leaves_the_rows_unchanged()
    {
        var ana = await client.CreateProfileAsync();
        await Read(await Send(ana.Token, Result(timeMs: 60_000, swaps: 12)));
        await Read(await Send(ana.Token, Result(timeMs: 50_000, swaps: 20)));
        await Read(await Send(ana.Token, Result(timeMs: 70_000, swaps: 8)));
        var before = Rows();
        factory.Clock.Advance(TimeSpan.FromMinutes(5));

        var answer = await Read(await Send(ana.Token, Result(timeMs: 50_000, swaps: 20)));

        Assert.Equal((false, false, false), answer);
        Assert.Equivalent(before, Rows(), strict: true);
    }

    [Fact]
    public async Task A_tie_in_the_first_key_is_broken_by_the_second()
    {
        var ana = await client.CreateProfileAsync();
        await Read(await Send(ana.Token, Result(timeMs: 60_000, swaps: 12)));

        var sameTimeFewerSwaps = await Read(await Send(ana.Token, Result(timeMs: 60_000, swaps: 11)));

        Assert.Equal((true, true, false), sameTimeFewerSwaps);
    }

    [Fact]
    public async Task A_new_best_stores_every_value_and_the_server_time()
    {
        var ana = await client.CreateProfileAsync();
        await Read(await Send(ana.Token, Result(timeMs: 60_000, swaps: 12, deaths: 3, roomRestarts: 1)));
        factory.Clock.Advance(TimeSpan.FromMinutes(1));

        await Read(await Send(ana.Token, Result(timeMs: 50_000, swaps: 20, deaths: 0, roomRestarts: 2)));

        var rows = Rows();
        var time = Assert.Single(rows, r => r.Mode == RankingMode.Time);
        var moves = Assert.Single(rows, r => r.Mode == RankingMode.Moves);
        Assert.Equal((ana.Id, "level-1", 50_000, 20, 0, 2), (time.ProfileId, time.LevelId, time.TimeMs, time.Swaps, time.Deaths, time.RoomRestarts));
        Assert.Equal(factory.Clock.GetUtcNow(), time.AchievedAt);
        Assert.Equal(new DateTimeOffset(2026, 11, 2, 11, 59, 0, TimeSpan.Zero), time.FinishedAt);
        Assert.Equal((60_000, 12, 3, 1), (moves.TimeMs, moves.Swaps, moves.Deaths, moves.RoomRestarts));
        Assert.Equal(factory.Clock.GetUtcNow() - TimeSpan.FromMinutes(1), moves.AchievedAt);
        Assert.All(rows, r => Assert.Null(r.GhostId));
    }

    // ---- G-SC 12 over HTTP: implausible results ----

    [Theory]
    [InlineData(3_999, 0, 0, 0)] // 4 rooms → at least 4 000 ms
    [InlineData(86_400_001, 0, 0, 0)]
    [InlineData(60_000, -1, 0, 0)]
    [InlineData(60_000, 10_001, 0, 0)]
    [InlineData(60_000, 0, -1, 0)]
    [InlineData(60_000, 0, 0, 10_001)]
    public async Task An_implausible_result_is_422_and_nothing_is_stored(int timeMs, int swaps, int deaths, int roomRestarts)
    {
        var ana = await client.CreateProfileAsync();

        var response = await Send(ana.Token, Result(timeMs: timeMs, swaps: swaps, deaths: deaths, roomRestarts: roomRestarts));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("ImplausibleResult", await response.ProblemCodeAsync());
        Assert.Empty(Rows());
    }

    [Fact]
    public async Task The_plausibility_floor_uses_the_room_count_of_the_level()
    {
        var ana = await client.CreateProfileAsync();
        await Read(await Send(ana.Token, Result("level-1")));
        await Read(await Send(ana.Token, Result("level-2")));

        // level-4 has 3 rooms: 3 000 ms is accepted although it would be too fast for level-1 (4 rooms).
        var answer = await Send(ana.Token, Result("level-4", timeMs: 3_000, swaps: 10_000));

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
    }

    // ---- G-SC 13, 18, 22: strict body ----

    [Theory]
    [InlineData("""{"levelId":"level-1","timeMs":60000,"swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"2026-11-02T11:59:00Z","ghost":"AAAA"}""")]
    [InlineData("""{"levelId":"level-1","timeMs":60000,"swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"2026-11-02T11:59:00Z","profileId":"00000000-0000-0000-0000-000000000001"}""")]
    [InlineData("""{"levelId":"level-1","timeMs":60000,"swaps":1,"deaths":0,"finishedAt":"2026-11-02T11:59:00Z"}""")]
    [InlineData("""{"timeMs":60000,"swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"2026-11-02T11:59:00Z"}""")]
    [InlineData("""{"levelId":null,"timeMs":60000,"swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"2026-11-02T11:59:00Z"}""")]
    [InlineData("""{"levelId":"level-1","timeMs":60000,"swaps":1,"deaths":0,"roomRestarts":0}""")]
    [InlineData("""{"levelId":"level-1","timeMs":1.5,"swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"2026-11-02T11:59:00Z"}""")]
    [InlineData("""{"levelId":"level-1","timeMs":"60000","swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"2026-11-02T11:59:00Z"}""")]
    [InlineData("""{"levelId":"level-1","timeMs":3000000000,"swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"2026-11-02T11:59:00Z"}""")]
    [InlineData("""{"levelId":"level-1","timeMs":1e30,"swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"2026-11-02T11:59:00Z"}""")]
    [InlineData("""{"levelId":"level-1","timeMs":null,"swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"2026-11-02T11:59:00Z"}""")]
    [InlineData("""{"levelId":"level-1","timeMs":60000,"swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"yesterday"}""")]
    [InlineData("""{"levelId":1,"timeMs":60000,"swaps":1,"deaths":0,"roomRestarts":0,"finishedAt":"2026-11-02T11:59:00Z"}""")]
    [InlineData("[]")]
    [InlineData("{ not json")]
    public async Task A_body_that_is_not_exactly_a_result_is_400_InvalidBody(string json)
    {
        var ana = await client.CreateProfileAsync();

        var response = await SendRaw(ana.Token, json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidBody", await response.ProblemCodeAsync());
        Assert.Empty(Rows());
    }

    [Fact]
    public async Task A_result_sent_with_A_token_is_never_shown_under_B()
    {
        var ana = await client.CreateProfileAsync("Ana");
        var bia = await client.CreateProfileAsync("Bia");

        await Read(await Send(ana.Token, Result()));

        var biaLevel1 = (await MyProgress(bia.Token))[0];
        Assert.Null(biaLevel1.BestTime);
        Assert.Null(biaLevel1.BestMoves);
        Assert.False((await MyProgress(bia.Token))[1].Unlocked);
        Assert.All(Rows(), r => Assert.Equal(ana.Id, r.ProfileId));
    }

    // ---- G-SC 16, 19: storage is bounded ----

    [Fact]
    public void The_best_result_table_has_a_nullable_GhostId_column()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        var ghost = db.Model.FindEntityType(typeof(BestResultEntity))!.FindProperty(nameof(BestResultEntity.GhostId))!;

        Assert.True(ghost.IsNullable);
        Assert.Contains("GhostId", DatabaseDump.AllColumns(db));
    }

    [Fact]
    public async Task A_thousand_uploads_leave_exactly_two_rows()
    {
        var ana = await client.CreateProfileAsync();

        for (var i = 0; i < 1_000; i++)
        {
            var response = await Send(ana.Token, Result(timeMs: 4_000 + (i * 7919 % 1_000_000), swaps: i * 31 % 10_000));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var rows = Rows();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal((ana.Id, "level-1"), (r.ProfileId, r.LevelId)));
    }

    // ---- G-SC 20, 25: no valid token, no row ----

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-real-token")]
    public async Task Without_a_valid_token_the_answer_is_401_and_no_row_is_written(string? token)
    {
        await client.CreateProfileAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/results") { Content = JsonContent.Create(Result()) };
        if (token is not null)
            request.WithToken(token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("InvalidToken", await response.ProblemCodeAsync());
        Assert.Empty(Rows());
    }

    [Fact]
    public async Task After_logout_the_token_cannot_send_results()
    {
        var ana = await client.CreateProfileAsync();
        await client.SendWithTokenAsync(HttpMethod.Delete, "/sessions/current", ana.Token);

        var response = await Send(ana.Token, Result());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(Rows());
    }

    [Fact]
    public async Task My_progress_needs_a_token()
    {
        var response = await client.GetAsync("/progress/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("InvalidToken", await response.ProblemCodeAsync());
    }

    // ---- G-SC 13, 26: the ghost routes do not exist yet ----

    [Theory]
    [InlineData("PUT", "/results/level-1/time/ghost")]
    [InlineData("GET", "/rankings/level-1/ghosts?mode=time")]
    public async Task The_ghost_routes_answer_404(string method, string url)
    {
        var ana = await client.CreateProfileAsync();
        var request = new HttpRequestMessage(new HttpMethod(method), url) { Content = new ByteArrayContent([1, 2, 3]) }.WithToken(ana.Token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- G-SC 27: refused results leave a trace ----

    [Fact]
    public async Task An_implausible_result_writes_one_security_event_without_the_body()
    {
        var ana = await client.CreateProfileAsync();

        await Send(ana.Token, Result("level-1", timeMs: 1_234, swaps: 5));

        var line = Assert.Single(factory.SecurityEvents, l => l.Values.GetValueOrDefault("Event") == "ResultRefused");
        Assert.Equal(ana.Id.ToString(), line.Values["ProfileId"]);
        Assert.Contains("level-1", line.Values["Detail"]);
        Assert.Contains("ImplausibleResult", line.Values["Detail"]);
        Assert.DoesNotContain("finishedAt", line.AllText);
        Assert.DoesNotContain("2026-11-02T11:59", line.AllText);
    }

    [Fact]
    public async Task A_result_for_a_locked_level_writes_one_security_event()
    {
        var ana = await client.CreateProfileAsync();

        await Send(ana.Token, Result("level-4"));

        var line = Assert.Single(factory.SecurityEvents, l => l.Values.GetValueOrDefault("Event") == "ResultRefused");
        Assert.Equal(ana.Id.ToString(), line.Values["ProfileId"]);
        Assert.Contains("level-4", line.Values["Detail"]);
        Assert.Contains("LevelLocked", line.Values["Detail"]);
    }

    [Fact]
    public async Task An_accepted_result_writes_no_security_event()
    {
        var ana = await client.CreateProfileAsync();

        await Read(await Send(ana.Token, Result()));

        Assert.DoesNotContain(factory.SecurityEvents, l => l.Values.GetValueOrDefault("Event") == "ResultRefused");
    }
}

/// <summary>G-SC 14: the <c>uploads</c> policy with the production number.</summary>
public sealed class UploadRateLimitTests : IDisposable
{
    private readonly TestCatalog catalog = new();

    public void Dispose() => catalog.Dispose();

    [Fact]
    public async Task The_31st_upload_by_one_profile_within_a_minute_is_429()
    {
        using var factory = new ApiFactory
        {
            Settings =
            {
                ["Progress:LevelsDirectory"] = catalog.Directory,
                ["Profiles:RateLimits:UploadsPerMinute"] = "30",
            },
        };
        var client = factory.CreateClient();
        var ana = await client.CreateProfileAsync("Ana");
        var bia = await client.CreateProfileAsync("Bia");
        var body = new { levelId = "level-1", timeMs = 60_000, swaps = 1, deaths = 0, roomRestarts = 0, finishedAt = "2026-11-02T11:59:00Z" };
        for (var i = 0; i < 30; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.SendWithTokenAsync(HttpMethod.Post, "/results", ana.Token, body)).StatusCode);

        var thirtyFirst = await client.SendWithTokenAsync(HttpMethod.Post, "/results", ana.Token, body);
        var otherProfile = await client.SendWithTokenAsync(HttpMethod.Post, "/results", bia.Token, body);

        Assert.Equal(HttpStatusCode.TooManyRequests, thirtyFirst.StatusCode);
        Assert.NotNull(thirtyFirst.Headers.RetryAfter);
        Assert.Equal(HttpStatusCode.OK, otherProfile.StatusCode);
    }
}
