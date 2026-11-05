using Godot;
using Swapping.Game.Pieces;
using Barrier = Swapping.Game.Pieces.Barrier;
using Swapping.Levels;
using Swapping.Levels.Model;
using Swapping.Rules;

namespace Swapping.Game;

/// <summary>
/// Loads a level from the catalog (<c>res://levels</c>), shows its scenery scene and builds every room of it
/// at once (user's request, 2026-11-04): each room has its pieces and its own <c>rules</c> <see cref="World"/>,
/// and they all keep working until the end of the level. The current room is the one whose exit grid the
/// player last crossed: it is where the player respawns and the only world the player swaps in. Rebuilding
/// one room is how that room restarts. No piece is hard-coded in a scene.
/// </summary>
public partial class LevelRunner : Node3D
{
    private sealed record RoomState(RoomDef Def, World World, Node3D Pieces);

    private Node3D _pieces = null!;
    private Node? _scenery;
    private readonly Dictionary<string, RoomState> _rooms = [];
    private RoomState? _current;

    public LevelFile Level { get; private set; } = null!;

    public RoomDef Room => _current?.Def!;

    public World World => _current?.World!;

    // The current room's pieces.
    public IReadOnlyList<Body> Bodies => Of<Body>(_current);

    public IReadOnlyList<KillZone> KillZones => Of<KillZone>(_current);

    public IReadOnlyList<FloorButton> Buttons => Of<FloorButton>(_current);

    public IReadOnlyList<Door> Doors => Of<Door>(_current);

    public IReadOnlyList<Barrier> Barriers => Of<Barrier>(_current);

    public Exit Exit => Of<Exit>(_current).Single();

    // Every room's pieces (they all stay alive).
    public IReadOnlyList<Barrier> AllBarriers => _rooms.Values.SelectMany(Of<Barrier>).ToList();

    /// <summary>Every body of the level with the room it belongs to (its bounds and kill zones).</summary>
    public IReadOnlyList<(Body Body, RoomDef Room)> AllBodies =>
        _rooms.Values.SelectMany(r => Of<Body>(r).Select(b => (b, r.Def))).ToList();

    public IEnumerable<KillZoneDef> AllKillZones => _rooms.Values.SelectMany(r => r.Def.KillZones);

    /// <summary>Every piece of the current room: objects, kill zones, buttons, doors, barriers and the exit.</summary>
    public int PieceCount => _current?.Pieces.GetChildCount() ?? 0;

    /// <summary>How many pieces the current room's file describes (plus its exit).</summary>
    public int ExpectedPieceCount =>
        Room.Objects.Count + Room.KillZones.Count + Room.Buttons.Count + Room.Doors.Count + Room.Barriers.Count + 1;

    /// <summary>How many rooms are built (alive) right now.</summary>
    public int BuiltRooms => _rooms.Count;

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
        FreeAllRooms();
        _scenery?.Free();
        _scenery = GD.Load<PackedScene>(Level.Scene).Instantiate();
        AddChild(_scenery);
    }

    /// <summary>Frees the scenery and every piece: no level loaded (back at the menu).</summary>
    public void Unload()
    {
        FreeAllRooms();
        _scenery?.QueueFree();
        _scenery = null;
    }

    /// <summary>Builds every room of the level from the file (a new run) and makes <paramref name="roomId"/> current.</summary>
    public void BuildAll(string roomId)
    {
        FreeAllRooms();
        foreach (var room in Level.Rooms)
            Build(room.Id);
        _current = _rooms[roomId];
    }

    /// <summary>
    /// Rebuilds one room from the file, with a new world, and makes it current (restart, death). The other
    /// rooms are not touched. With no room built yet (first call of a level), builds them all.
    /// </summary>
    public void BuildRoom(string roomId)
    {
        if (_rooms.Count == 0)
        {
            BuildAll(roomId);
            return;
        }
        if (_rooms.TryGetValue(roomId, out var old))
        {
            _pieces.RemoveChild(old.Pieces);
            old.Pieces.QueueFree();
        }
        Build(roomId);
        _current = _rooms[roomId];
    }

    /// <summary>The player crossed into <paramref name="roomId"/>: it becomes current, as it is (nothing rebuilt).</summary>
    public void EnterRoom(string roomId) => _current = _rooms[roomId];

    private void Build(string roomId)
    {
        var def = Level.Rooms.First(r => r.Id == roomId);
        var pieces = new Node3D { Name = $"Room_{roomId.Replace(".", "_")}" };
        foreach (var obj in def.Objects)
            pieces.AddChild(Body.Create(obj));
        foreach (var zone in def.KillZones)
            pieces.AddChild(KillZone.Create(zone));
        foreach (var button in def.Buttons)
            pieces.AddChild(FloorButton.Create(button));
        foreach (var door in def.Doors)
            pieces.AddChild(Door.Create(door));
        foreach (var barrier in def.Barriers)
            pieces.AddChild(Barrier.Create(barrier));
        pieces.AddChild(Exit.Create(def.Id, def.Exit));
        _pieces.AddChild(pieces);
        _rooms[roomId] = new RoomState(def, RulesMapping.BuildWorld(Level, roomId), pieces);
    }

    private void FreeAllRooms()
    {
        if (_pieces is null)
            return;
        foreach (var child in _pieces.GetChildren())
        {
            _pieces.RemoveChild(child);
            child.QueueFree();
        }
        _rooms.Clear();
        _current = null;
    }

    private static List<T> Of<T>(RoomState? room) where T : Node =>
        room is null || !IsInstanceValid(room.Pieces) ? [] : room.Pieces.GetChildren().OfType<T>().ToList();

    /// <summary>Buttons read what rests on them; each door opens while all its buttons are pressed — in every room.</summary>
    public override void _PhysicsProcess(double delta)
    {
        foreach (var room in _rooms.Values)
        {
            var buttons = Of<FloorButton>(room);
            foreach (var button in buttons)
                button.Refresh();
            var pressed = buttons.Where(b => b.IsPressed).Select(b => b.Def.Id).ToHashSet();
            foreach (var door in Of<Door>(room))
                door.SetOpen(Swapping.GameLogic.ButtonState.IsDoorOpen(door.Def.OpensWith, pressed));
        }
    }

    /// <summary>The eye of a player standing at the room spawn, looking along the spawn yaw.</summary>
    public Transform3D SpawnEye()
    {
        var basis = Basis.FromEuler(new Vector3(0, Mathf.DegToRad((float)Room.Spawn.YawDegrees), 0));
        return new Transform3D(basis, Room.Spawn.Position.ToGodot() + new Vector3(0, 1.6f, 0));
    }
}
