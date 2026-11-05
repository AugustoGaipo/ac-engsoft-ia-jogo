using Swapping.GameLogic;
using Swapping.GameLogic.Online;

namespace Swapping.Api.Tests.GameClient;

/// <summary>
/// The game's client against the real API in memory (user, 2026-11-05: account with a code generated on the
/// spot, results sent when a level ends, the ranking per level in the menu). Same JSON on both sides.
/// </summary>
public sealed class GameApiContractTests : IDisposable
{
    private readonly ApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    private GameApi Api() => new(factory.CreateClient());

    private static RunResult Level1(int timeMs, int swaps) => new("level-1", timeMs, swaps, 0, 0, new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Creating_an_account_returns_a_generated_code_that_logs_in()
    {
        var api = Api();
        var created = await api.CreateAccountAsync("Augusto");

        Assert.True(created.Ok, created.Code);
        Assert.Matches("^[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}$", created.Value!.AccessCode);
        var login = await api.LoginAsync("Augusto", created.Value.AccessCode);
        Assert.True(login.Ok, login.Code);
        Assert.False(string.IsNullOrEmpty(login.Value!.Token));
    }

    [Fact]
    public async Task A_wrong_code_and_a_taken_name_come_back_as_codes()
    {
        var api = Api();
        Assert.True((await api.CreateAccountAsync("Bia")).Ok);

        var wrong = await api.LoginAsync("Bia", "0000-0000-0000-0000");
        var taken = await api.CreateAccountAsync("Bia");

        Assert.Equal((401, "InvalidCredentials"), (wrong.Status, wrong.Code));
        Assert.Equal((409, "NameTaken"), (taken.Status, taken.Code));
    }

    [Fact]
    public async Task A_finished_level_goes_into_the_ranking_with_my_place()
    {
        var api = Api();
        var ana = (await api.CreateAccountAsync("Ana")).Value!;
        var caio = (await api.CreateAccountAsync("Caio")).Value!;

        Assert.True((await api.SubmitAsync(ana.Token, Level1(90_000, 6))).Ok);
        Assert.True((await api.SubmitAsync(caio.Token, Level1(60_000, 9))).Ok);

        var ranking = await api.RankingAsync("level-1");
        Assert.True(ranking.Ok, ranking.Code);
        Assert.Equal(["Caio", "Ana"], ranking.Value!.Select(l => l.Name));
        Assert.Equal((1, 60_000, 9), (ranking.Value![0].Rank, ranking.Value[0].TimeMs, ranking.Value[0].Swaps));
        var mine = await api.MyRankAsync(ana.Token, "level-1");
        Assert.Equal((2, 2), (mine.Value!.Rank, mine.Value.TotalPlayers));
    }

    [Fact]
    public async Task No_result_yet_means_no_place_and_an_empty_ranking()
    {
        var api = Api();
        var dani = (await api.CreateAccountAsync("Dani")).Value!;

        Assert.Empty((await api.RankingAsync("level-2")).Value!);
        var mine = await api.MyRankAsync(dani.Token, "level-2");
        Assert.True(mine.Ok);
        Assert.Null(mine.Value!.Place);
    }
}
