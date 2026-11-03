using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Swapping.Api.Data;
using Swapping.Api.Progress;

namespace Swapping.Api.Tests.Progress;

/// <summary>
/// <c>GET /rankings/{levelId}</c> and <c>GET /rankings/{levelId}/me</c> with the test catalog
/// level-1 (4 rooms), level-2 (3 rooms), level-4 (2 rooms).
/// </summary>
public sealed class RankingTests : IDisposable
{
    private readonly TestCatalog catalog = new();
    private readonly ApiFactory factory;
    private readonly HttpClient client;

    public RankingTests()
    {
        factory = new ApiFactory { Settings = { ["Progress:LevelsDirectory"] = catalog.Directory } };
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        factory.Dispose();
        catalog.Dispose();
    }

    /// <summary>Sends a result and moves the clock 1 s, so every upload has its own server time.</summary>
    private async Task Upload(NewProfile profile, int timeMs, int swaps, string levelId = "level-1")
    {
        var response = await client.SendWithTokenAsync(HttpMethod.Post, "/results", profile.Token,
            new { levelId, timeMs, swaps, deaths = 0, roomRestarts = 0, finishedAt = "2026-11-02T11:59:00Z" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        factory.Clock.Advance(TimeSpan.FromSeconds(1));
    }

    private sealed record Entry(int Rank, string Name, string Icon, int TimeMs, int Swaps);

    private async Task<List<Entry>> Ranking(string query, string levelId = "level-1")
    {
        var response = await client.GetAsync($"/rankings/{levelId}?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("entries").EnumerateArray().Select(e => new Entry(
            e.GetProperty("rank").GetInt32(), e.GetProperty("name").GetString()!, e.GetProperty("icon").GetString()!,
            e.GetProperty("timeMs").GetInt32(), e.GetProperty("swaps").GetInt32())).ToList();
    }

    private sealed record Mine(int Rank, int TimeMs, int Swaps, int TotalPlayers);

    private async Task<Mine> MyRank(NewProfile profile, string mode, string levelId = "level-1")
    {
        var response = await client.SendWithTokenAsync(HttpMethod.Get, $"/rankings/{levelId}/me?mode={mode}", profile.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(new[] { "rank", "timeMs", "swaps", "totalPlayers" }, root.EnumerateObject().Select(p => p.Name));
        return new Mine(root.GetProperty("rank").GetInt32(), root.GetProperty("timeMs").GetInt32(),
            root.GetProperty("swaps").GetInt32(), root.GetProperty("totalPlayers").GetInt32());
    }

    private async Task<(NewProfile A, NewProfile B, NewProfile C)> ThreePlayers()
    {
        var a = await client.CreateProfileAsync("Ana", "icon-01");
        var b = await client.CreateProfileAsync("Bia", "icon-02");
        var c = await client.CreateProfileAsync("Caio", "icon-03");
        await Upload(a, 50_000, 9);
        await Upload(b, 50_000, 7);
        await Upload(c, 40_000, 30);
        return (a, b, c);
    }

    // ---- G-SC 7: the two orders over HTTP ----

    [Fact]
    public async Task The_time_ranking_orders_by_time_then_by_swaps()
    {
        await ThreePlayers();

        var entries = await Ranking("mode=time");

        Assert.Equal(new[] { "Caio", "Bia", "Ana" }, entries.Select(e => e.Name));
        Assert.Equal(new[] { 1, 2, 3 }, entries.Select(e => e.Rank));
    }

    [Fact]
    public async Task The_moves_ranking_orders_by_swaps_then_by_time()
    {
        await ThreePlayers();

        var entries = await Ranking("mode=moves");

        Assert.Equal(new[] { "Bia", "Ana", "Caio" }, entries.Select(e => e.Name));
        Assert.Equal(new[] { 1, 2, 3 }, entries.Select(e => e.Rank));
    }

    [Fact]
    public async Task Each_mode_shows_the_best_row_of_that_mode()
    {
        var ana = await client.CreateProfileAsync("Ana");
        await Upload(ana, 60_000, 12);
        await Upload(ana, 50_000, 20);
        await Upload(ana, 70_000, 8);

        Assert.Equal(new Entry(1, "Ana", "icon-01", 50_000, 20), Assert.Single(await Ranking("mode=time")));
        Assert.Equal(new Entry(1, "Ana", "icon-01", 70_000, 8), Assert.Single(await Ranking("mode=moves")));
    }

    [Fact]
    public async Task A_ranking_only_has_rows_of_its_own_level()
    {
        var ana = await client.CreateProfileAsync("Ana");
        var bia = await client.CreateProfileAsync("Bia");
        await Upload(ana, 60_000, 12);
        await Upload(bia, 50_000, 10);
        await Upload(bia, 30_000, 5, "level-2");

        Assert.Equal(new[] { "Bia" }, (await Ranking("mode=time", "level-2")).Select(e => e.Name));
        Assert.Equal(new[] { "Bia", "Ana" }, (await Ranking("mode=time")).Select(e => e.Name));
    }

    [Fact]
    public async Task A_level_with_no_results_has_an_empty_ranking()
    {
        Assert.Empty(await Ranking("mode=time", "level-4"));
    }

    // ---- G-SC 8: first upload wins ties ----

    [Fact]
    public async Task With_identical_results_the_first_upload_ranks_higher_in_both_modes()
    {
        // Bia's profile is older, but Ana uploads first.
        var bia = await client.CreateProfileAsync("Bia");
        var ana = await client.CreateProfileAsync("Ana");
        await Upload(ana, 50_000, 9);
        await Upload(bia, 50_000, 9);

        Assert.Equal(new[] { "Ana", "Bia" }, (await Ranking("mode=time")).Select(e => e.Name));
        Assert.Equal(new[] { "Ana", "Bia" }, (await Ranking("mode=moves")).Select(e => e.Name));
        Assert.Equal(1, (await MyRank(ana, "time")).Rank);
        Assert.Equal(2, (await MyRank(bia, "moves")).Rank);
    }

    // ---- G-SC 9: entries carry no private data ----

    [Fact]
    public async Task The_answer_has_the_level_the_mode_and_entries_with_only_five_fields()
    {
        var ana = await client.CreateProfileAsync("Ana", "icon-07");
        await Upload(ana, 50_000, 9);

        var response = await client.GetAsync("/rankings/level-1?mode=moves");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(new[] { "levelId", "mode", "entries" }, root.EnumerateObject().Select(p => p.Name));
        Assert.Equal("level-1", root.GetProperty("levelId").GetString());
        Assert.Equal("moves", root.GetProperty("mode").GetString());
        var entry = Assert.Single(root.GetProperty("entries").EnumerateArray().ToList());
        Assert.Equal(new[] { "rank", "name", "icon", "timeMs", "swaps" }, entry.EnumerateObject().Select(p => p.Name));
        Assert.Equal("icon-07", entry.GetProperty("icon").GetString());
        Assert.DoesNotContain(ana.Id.ToString(), body.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rankings_are_public_and_need_no_token()
    {
        var response = await client.GetAsync("/rankings/level-1?mode=time");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---- G-SC 10: top ----

    [Fact]
    public async Task By_default_a_ranking_has_at_most_20_entries_and_top_changes_it()
    {
        for (var i = 1; i <= 21; i++)
            await Upload(await client.CreateProfileAsync($"Player{i:00}"), 40_000 + i, 10);

        var byDefault = await Ranking("mode=time");
        Assert.Equal(20, byDefault.Count);
        Assert.Equal("Player01", byDefault[0].Name);
        Assert.Equal(Enumerable.Range(1, 20), byDefault.Select(e => e.Rank));

        Assert.Equal(new[] { "Player01" }, (await Ranking("mode=time&top=1")).Select(e => e.Name));
        Assert.Equal(21, (await Ranking("mode=time&top=100")).Count);
    }

    [Theory]
    [InlineData("mode=time&top=0")]
    [InlineData("mode=time&top=101")]
    [InlineData("mode=time&top=-1")]
    [InlineData("mode=time&top=abc")]
    [InlineData("mode=time&top=1.5")]
    [InlineData("mode=time&top=")]
    [InlineData("mode=time&top=99999999999")]
    [InlineData("mode=time&top=5&top=6")]
    [InlineData("")]
    [InlineData("mode=")]
    [InlineData("mode=TIME;DROP")]
    [InlineData("mode=TIME")]
    [InlineData("mode=speed")]
    [InlineData("mode=time&mode=moves")]
    public async Task A_bad_mode_or_top_is_400_InvalidQuery(string query)
    {
        var response = await client.GetAsync($"/rankings/level-1?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidQuery", await response.ProblemCodeAsync());
    }

    // ---- G-SC 11: my rank ----

    [Fact]
    public async Task My_rank_and_the_number_of_players_in_both_modes()
    {
        var (a, b, c) = await ThreePlayers();

        Assert.Equal(new Mine(3, 50_000, 9, 3), await MyRank(a, "time"));
        Assert.Equal(new Mine(2, 50_000, 9, 3), await MyRank(a, "moves"));
        Assert.Equal(new Mine(1, 40_000, 30, 3), await MyRank(c, "time"));
        Assert.Equal(new Mine(3, 40_000, 30, 3), await MyRank(c, "moves"));
        Assert.Equal(new Mine(1, 50_000, 7, 3), await MyRank(b, "moves"));
    }

    [Theory]
    [InlineData("time")]
    [InlineData("moves")]
    public async Task My_rank_matches_my_position_in_the_full_ranking(string mode)
    {
        // Ties on the first and the second key, so every tie-break is used.
        var rows = new (int TimeMs, int Swaps)[]
        {
            (50_000, 9), (50_000, 9), (50_000, 7), (40_000, 30), (40_000, 9), (60_000, 7), (50_000, 30),
        };
        var players = new List<NewProfile>();
        for (var i = 0; i < rows.Length; i++)
        {
            var player = await client.CreateProfileAsync($"Player{i}");
            players.Add(player);
            await Upload(player, rows[i].TimeMs, rows[i].Swaps);
        }

        var ranking = await Ranking($"mode={mode}&top=100");

        foreach (var player in players)
        {
            var mine = await MyRank(player, mode);
            Assert.Equal(ranking.Single(e => e.Name == player.Name).Rank, mine.Rank);
            Assert.Equal(rows.Length, mine.TotalPlayers);
        }
    }

    [Fact]
    public async Task My_rank_without_a_result_is_404_NoResult()
    {
        var ana = await client.CreateProfileAsync("Ana");
        await Upload(ana, 50_000, 9);

        var response = await client.SendWithTokenAsync(HttpMethod.Get, "/rankings/level-2/me?mode=time", ana.Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NoResult", await response.ProblemCodeAsync());
    }

    [Fact]
    public async Task My_rank_needs_a_token()
    {
        var response = await client.GetAsync("/rankings/level-1/me?mode=time");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("InvalidToken", await response.ProblemCodeAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("mode=TIME;DROP")]
    [InlineData("mode=time&mode=moves")]
    public async Task My_rank_with_a_bad_mode_is_400_InvalidQuery(string query)
    {
        var ana = await client.CreateProfileAsync("Ana");

        var response = await client.SendWithTokenAsync(HttpMethod.Get, $"/rankings/level-1/me?{query}", ana.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidQuery", await response.ProblemCodeAsync());
    }

    // ---- G-SC 18: one profile's result never shows under another ----

    [Fact]
    public async Task A_result_sent_with_A_token_never_appears_under_B()
    {
        var ana = await client.CreateProfileAsync("Ana", "icon-01");
        var bia = await client.CreateProfileAsync("Bia", "icon-02");
        await Upload(ana, 50_000, 9);

        Assert.Equal(new Entry(1, "Ana", "icon-01", 50_000, 9), Assert.Single(await Ranking("mode=time")));
        var biaRank = await client.SendWithTokenAsync(HttpMethod.Get, "/rankings/level-1/me?mode=time", bia.Token);
        Assert.Equal(HttpStatusCode.NotFound, biaRank.StatusCode);
    }

    // ---- G-SC 21: injection through the route ----

    [Theory]
    [InlineData("..%2F..%2Fetc")]
    [InlineData("x'%20OR%201=1")]
    [InlineData("level-3")]
    [InlineData("LEVEL-1")]
    public async Task An_id_not_in_the_catalog_is_404_UnknownLevel(string levelId)
    {
        var ana = await client.CreateProfileAsync("Ana");
        await Upload(ana, 50_000, 9);

        var ranking = await client.GetAsync($"/rankings/{levelId}?mode=time");
        var mine = await client.SendWithTokenAsync(HttpMethod.Get, $"/rankings/{levelId}/me?mode=time", ana.Token);

        Assert.Equal(HttpStatusCode.NotFound, ranking.StatusCode);
        Assert.Equal("UnknownLevel", await ranking.ProblemCodeAsync());
        Assert.Equal(HttpStatusCode.NotFound, mine.StatusCode);
        Assert.Equal("UnknownLevel", await mine.ProblemCodeAsync());
    }

    // ---- G-SC 23: the two ranking indexes ----

    [Fact]
    public void The_model_has_one_index_per_ranking_order()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var indexes = db.Model.FindEntityType(typeof(BestResultEntity))!.GetIndexes()
            .Select(i => string.Join(",", i.Properties.Select(p => p.Name))).ToList();

        Assert.Contains("LevelId,Mode,TimeMs,Swaps,AchievedAt", indexes);
        Assert.Contains("LevelId,Mode,Swaps,TimeMs,AchievedAt", indexes);
    }

    // ---- G-SC 24: no CORS ----

    [Fact]
    public async Task A_page_from_another_origin_gets_no_CORS_permission_on_rankings_or_results()
    {
        var get = new HttpRequestMessage(HttpMethod.Get, "/rankings/level-1?mode=time");
        get.Headers.Add("Origin", "https://evil.example");
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/results");
        preflight.Headers.Add("Origin", "https://evil.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");

        var getResponse = await client.SendAsync(get);
        var preflightResponse = await client.SendAsync(preflight);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.False(getResponse.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(preflightResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
