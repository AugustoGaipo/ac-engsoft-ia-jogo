using Godot;
using Swapping.GameLogic;
using Swapping.GameLogic.Online;

namespace Swapping.Game;

/// <summary>
/// The online screens of the menu (user, 2026-11-05): "Conta" — create an account (the server generates the
/// access code, shown once with a copy button) or log in with name + code — and "Ranking" — the top 10 of each
/// level by time, and my place when logged in.
/// </summary>
public partial class Menu
{
    private VBoxContainer _accountScreen = null!;
    private VBoxContainer _rankingScreen = null!;
    private Label _accountStatus = null!;
    private LineEdit _nameInput = null!;
    private LineEdit _codeInput = null!;
    private Label _codeShown = null!;
    private Button _copyCode = null!;
    private Control _loggedOut = null!;
    private Control _loggedIn = null!;
    private Label _who = null!;
    private Label _rankingTitle = null!;
    private Label _rankingLines = null!;
    private string _rankingLevel = "";

    /// <summary>Set by the game; without it the online screens only say the server is not set.</summary>
    public Online? Online { get; set; }

    /// <summary>Text of the ranking list shown (headless checks).</summary>
    public string RankingText => _rankingLines.Text;

    public string AccountStatus => _accountStatus.Text;

    private void BuildOnline()
    {
        _accountScreen = NewScreen("Conta");
        _who = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _accountScreen.AddChild(_who);

        var loggedOut = new VBoxContainer();
        loggedOut.AddThemeConstantOverride("separation", 10);
        _nameInput = new LineEdit { PlaceholderText = "nome (3 a 16 letras ou números)", CustomMinimumSize = new Vector2(380, 44), MaxLength = 16 };
        _codeInput = new LineEdit { PlaceholderText = "código (só para entrar)", CustomMinimumSize = new Vector2(380, 44), MaxLength = 19 };
        loggedOut.AddChild(_nameInput);
        loggedOut.AddChild(_codeInput);
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 10);
        var create = new Button { Text = "Criar conta", CustomMinimumSize = new Vector2(185, 48) };
        create.Pressed += () => _ = CreateAccount();
        var enter = new Button { Text = "Entrar", CustomMinimumSize = new Vector2(185, 48) };
        enter.Pressed += () => _ = Login();
        row.AddChild(create);
        row.AddChild(enter);
        loggedOut.AddChild(row);
        _loggedOut = loggedOut;
        _accountScreen.AddChild(loggedOut);

        // The generated code, shown once after creating the account.
        _codeShown = new Label { HorizontalAlignment = HorizontalAlignment.Center, Visible = false };
        _codeShown.AddThemeFontSizeOverride("font_size", 30);
        _codeShown.AddThemeColorOverride("font_color", LabTheme.Accent);
        _accountScreen.AddChild(_codeShown);
        _copyCode = new Button { Text = "Copiar código", CustomMinimumSize = new Vector2(280, 44), Visible = false };
        _copyCode.Pressed += () =>
        {
            DisplayServer.ClipboardSet(_codeShown.Text);
            _accountStatus.Text = "Código copiado. Guarde: ele não aparece de novo.";
        };
        _accountScreen.AddChild(_copyCode);

        var logout = new Button { Text = "Sair da conta", CustomMinimumSize = new Vector2(280, 48) };
        logout.Pressed += () =>
        {
            Online?.Logout();
            _codeShown.Visible = _copyCode.Visible = false;
            _accountStatus.Text = "";
            RefreshAccount();
        };
        _loggedIn = logout;
        _accountScreen.AddChild(logout);

        _accountStatus = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(380, 0) };
        _accountStatus.AddThemeColorOverride("font_color", LabTheme.Muted);
        _accountScreen.AddChild(_accountStatus);
        AddButton(_accountScreen, "Voltar", () => Show("main"));

        _rankingScreen = NewScreen("Ranking");
        var tabs = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        tabs.AddThemeConstantOverride("separation", 8);
        foreach (var level in _levels)
        {
            var id = level.Id;
            var tab = new Button { Text = $"Nível {level.Number}", CustomMinimumSize = new Vector2(120, 44) };
            tab.Pressed += () => _ = LoadRanking(id);
            tabs.AddChild(tab);
        }
        _rankingScreen.AddChild(tabs);
        _rankingTitle = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _rankingTitle.AddThemeFontSizeOverride("font_size", 24);
        _rankingScreen.AddChild(_rankingTitle);
        _rankingLines = new Label { CustomMinimumSize = new Vector2(460, 300) };
        _rankingLines.AddThemeFontSizeOverride("font_size", 20);
        _rankingScreen.AddChild(_rankingLines);
        AddButton(_rankingScreen, "Voltar", () => Show("main"));
    }

    private void ShowOnline(string screen)
    {
        Holder(_accountScreen).Visible = screen == "account";
        Holder(_rankingScreen).Visible = screen == "ranking";
        if (screen == "account")
            RefreshAccount();
        if (screen == "ranking")
            _ = LoadRanking(_rankingLevel.Length > 0 ? _rankingLevel : _levels[0].Id);
    }

    private void RefreshAccount()
    {
        var session = Online?.Session;
        _who.Text = session is null ? "Sem conta: os resultados ficam só neste computador." : $"Conectado como {session.Name}";
        _loggedOut.Visible = session is null;
        _loggedIn.Visible = session is not null;
    }

    private async Task CreateAccount()
    {
        if (Online is null)
            return;
        _accountStatus.Text = "Criando...";
        var created = await Online.CreateAsync(_nameInput.Text);
        if (created.Value is { } account)
        {
            _codeShown.Text = account.AccessCode;
            _codeShown.Visible = _copyCode.Visible = true;
            _accountStatus.Text = "Conta criada! Este é o seu código para entrar em outro computador. Ele só aparece agora: anote ou copie.";
        }
        else
            _accountStatus.Text = OnlineText.Error(created.Status, created.Code);
        RefreshAccount();
    }

    private async Task Login()
    {
        if (Online is null)
            return;
        _accountStatus.Text = "Entrando...";
        var login = await Online.LoginAsync(_nameInput.Text, _codeInput.Text);
        _accountStatus.Text = login.Ok ? "" : OnlineText.Error(login.Status, login.Code);
        _codeInput.Text = "";
        RefreshAccount();
    }

    private async Task LoadRanking(string levelId)
    {
        _rankingLevel = levelId;
        var level = _levels.First(l => l.Id == levelId);
        _rankingTitle.Text = $"Nível {level.Number} · {level.Name} — melhor tempo";
        if (Online is null)
            return;
        _rankingLines.Text = "Carregando...";
        var ranking = await Online.Api.RankingAsync(levelId);
        if (_rankingLevel != levelId)
            return;                                 // another tab was chosen meanwhile
        if (ranking.Value is not { } lines)
        {
            _rankingLines.Text = OnlineText.Error(ranking.Status, ranking.Code);
            return;
        }
        var text = lines.Count == 0
            ? "Ninguém terminou este nível ainda."
            : string.Join("\n", lines.Select(l => $"{l.Rank,2}º  {l.Name,-16}  {RunClock.Format(l.TimeMs)}   {l.Swaps} trocas"));
        if (Online.Session is { } session)
        {
            var mine = await Online.Api.MyRankAsync(session.Token, levelId);
            if (_rankingLevel != levelId)
                return;
            text += mine.Value switch
            {
                { Place: { } place, TotalPlayers: var total } => $"\n\nVocê: {place}º de {total}",
                { Place: null } => "\n\nVocê ainda não terminou este nível.",
                _ => "",
            };
        }
        _rankingLines.Text = text;
    }
}
