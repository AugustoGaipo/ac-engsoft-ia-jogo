using System.Text.Json;
using Godot;
using Swapping.GameLogic;
using Swapping.GameLogic.Online;

namespace Swapping.Game;

/// <summary>
/// The game online (user, 2026-11-05): the account (the session token is kept in <c>user://account.json</c>;
/// the access code never is), results sent when a level ends, and the rankings for the menu. The server is
/// the API (<c>dotnet run --project src/Swapping.Api</c>), at <c>SWAPPING_API</c> or http://127.0.0.1:5257.
/// </summary>
public sealed class Online
{
    public const string DefaultServer = "http://127.0.0.1:5257";
    private readonly string _path;

    public Online(string accountPath = "user://account.json")
    {
        _path = accountPath;
        var server = System.Environment.GetEnvironmentVariable("SWAPPING_API") is { Length: > 0 } custom ? custom : DefaultServer;
        Api = new GameApi(new System.Net.Http.HttpClient { BaseAddress = new System.Uri(server), Timeout = System.TimeSpan.FromSeconds(4) });
        Session = Load();
    }

    public GameApi Api { get; }

    public Session? Session { get; private set; }

    public async Task<ApiAnswer<NewAccount>> CreateAsync(string name)
    {
        var created = await Api.CreateAccountAsync(name.Trim());
        if (created.Value is { } account)
            Keep(new Session(account.Name, account.Token));
        return created;
    }

    public async Task<ApiAnswer<Session>> LoginAsync(string name, string code)
    {
        var login = await Api.LoginAsync(name.Trim(), code.Trim());
        if (login.Value is { } session)
            Keep(session);
        return login;
    }

    public void Logout()
    {
        Session = null;
        if (Godot.FileAccess.FileExists(_path))
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(_path));
    }

    /// <summary>Sends a finished level; returns the line for the end screen.</summary>
    public async Task<string> SendAsync(RunResult result)
    {
        if (Session is not { } session)
            return "Entre numa conta (menu › Conta) para aparecer no ranking.";
        var sent = await Api.SubmitAsync(session.Token, result);
        if (sent.Ok)
            return "Enviado ao ranking.";
        if (sent.Status == 401)
            Logout();
        return $"Ranking: {OnlineText.Error(sent.Status, sent.Code)}.";
    }

    private void Keep(Session session)
    {
        Session = session;
        using var file = Godot.FileAccess.Open(_path, Godot.FileAccess.ModeFlags.Write);
        file?.StoreString(JsonSerializer.Serialize(new Saved(session.Name, session.Token)));
    }

    private Session? Load()
    {
        if (!Godot.FileAccess.FileExists(_path))
            return null;
        try
        {
            return JsonSerializer.Deserialize<Saved>(Godot.FileAccess.GetFileAsString(_path)) is { Name: { } n, Token: { } t } ? new Session(n, t) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record Saved(string? Name, string? Token);
}
