using Godot;
using Swapping.Game.Pieces;
using Barrier = Swapping.Game.Pieces.Barrier;
using Swapping.Levels;
using Swapping.Levels.Model;
using Swapping.Rules;

namespace Swapping.Game;

/// <summary>
/// Loads a level from the catalog (<c>res://levels</c>), shows its scenery scene and builds one room at a
/// time from the level file: the pieces and a new <c>rules</c> <see cref="World"/>. Building a room again is
/// how a room restarts. No piece is hard-coded in a scene.
/// </summary>
public partial class LevelRunner : Node3D
{
    private Node3D _pieces = null!;
    private Node? _scenery;

    public LevelFile Level { get; private set; } = null!;

    public RoomDef Room { get; private set; } = null!;

    public World World { get; private set; } = null!;

    public IReadOnlyList<Body> Bodies => _pieces.GetChildren().OfType<Body>().ToList();

    public IReadOnlyList<KillZone> KillZones => _pieces.GetChildren().OfType<KillZone>().ToList();

    public IReadOnlyList<FloorButton> Buttons => _pieces.GetChildren().OfType<FloorButton>().ToList();

    public IReadOnlyList<Door> Doors => _pieces.GetChildren().OfType<Door>().ToList();

    public IReadOnlyList<Barrier> Barriers => _pieces.GetChildren().OfType<Barrier>().ToList();

    public Exit Exit => _pieces.GetChildren().OfType<Exit>().Single();

    /// <summary>Every piece of the room: objects, kill zones, buttons, doors, barriers and the exit.</summary>
    public int PieceCount => _pieces.GetChildCount();

    /// <summary>How many pieces the current room's file describes (plus its exit).</summary>
    public int ExpectedPieceCount =>
        Room.Objects.Count + Room.KillZones.Count + Room.Buttons.Count + Room.Doors.Count + Room.Barriers.Count + 1;

    public override void _Ready()
    {
        _pieces = new Node3D { Name = "Pieces" };
        AddChild(_pieces);
    }

    /// <summary>Loads the level and its scenery. Throws with the catalog errors if the files are broken.</summary>
    public void LoadLevel(string levelId)
    {
        var catalog = LevelCatalog.Load(ProjectSettings.GlobalizePath("res://levels"));
        if (catalog.Errors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", catalog.Errors.Select(e => $"{e.Path}: {e.Code} {e.Message}")));
        Level = catalog.Levels.FirstOrDefault(l => l.Id == levelId)
                ?? throw new KeyNotFoundException($"Level '{levelId}' is not in the catalog.");

        ShowScenery();
    }

    /// <summary>The test room (game/sandbox): not in the catalog, never seen by the API.</summary>
    public const string SandboxFile = "res://sandbox/sandbox.json";

    /// <summary>
    /// Loads the sandbox with the same parser and validator as the levels. Only the level number and id
    /// rules are ignored: a test room must not take a real level number.
    /// </summary>
    public void LoadSandbox()
    {
        var (level, parseErrors) = LevelParser.Parse(System.IO.File.ReadAllText(ProjectSettings.GlobalizePath(SandboxFile)));
        var errors = parseErrors.Concat(level is null ? [] : LevelValidator.Validate(level))
            .Where(e => !(e.Path is "id" or "number" && e.Code is LevelErrorCode.BadId or LevelErrorCode.NumberOutOfRange))
            .ToList();
        if (level is null || errors.Count > 0)
            throw new InvalidOperationException("sandbox.json: " + string.Join("; ", errors.Select(e => $"{e.Path}: {e.Code} {e.Message}")));
        Level = level;
        ShowScenery();
    }

    private void ShowScenery()
    {
        _scenery?.Free();
        _scenery = GD.Load<PackedScene>(Level.Scene).Instantiate();
        AddChild(_scenery);
    }

    /// <summary>Frees the current pieces and builds the room again from the file, with a new world.</summary>
    public void BuildRoom(string roomId)
    {
        foreach (var child in _pieces.GetChildren())
        {
            _pieces.RemoveChild(child);
            child.QueueFree();
        }

        Room = Level.Rooms.First(r => r.Id == roomId);
        World = RulesMapping.BuildWorld(Level, roomId);
        foreach (var obj in Room.Objects)
            _pieces.AddChild(Body.Create(obj));
        foreach (var zone in Room.KillZones)
            _pieces.AddChild(KillZone.Create(zone));
        foreach (var button in Room.Buttons)
            _pieces.AddChild(FloorButton.Create(button));
        foreach (var door in Room.Doors)
            _pieces.AddChild(Door.Create(door));
        foreach (var barrier in Room.Barriers)
            _pieces.AddChild(Barrier.Create(barrier));
        _pieces.AddChild(Exit.Create(Room.Id, Room.Exit));
    }

    /// <summary>Buttons read what rests on them; each door opens while all its buttons are pressed.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_pieces is null)
            return;
        foreach (var button in Buttons)
            button.Refresh();
        var pressed = Buttons.Where(b => b.IsPressed).Select(b => b.Def.Id).ToHashSet();
        foreach (var door in Doors)
            door.SetOpen(Swapping.GameLogic.ButtonState.IsDoorOpen(door.Def.OpensWith, pressed));
    }

    /// <summary>The eye of a player standing at the room spawn, looking along the spawn yaw.</summary>
    public Transform3D SpawnEye()
    {
        var basis = Basis.FromEuler(new Vector3(0, Mathf.DegToRad((float)Room.Spawn.YawDegrees), 0));
        return new Transform3D(basis, Room.Spawn.Position.ToGodot() + new Vector3(0, 1.6f, 0));
    }
}
