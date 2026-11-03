using Godot;
using Swapping.Game.Pieces;
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

    public int PieceCount => _pieces.GetChildCount();

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
    }

    /// <summary>The eye of a player standing at the room spawn, looking along the spawn yaw.</summary>
    public Transform3D SpawnEye()
    {
        var basis = Basis.FromEuler(new Vector3(0, Mathf.DegToRad((float)Room.Spawn.YawDegrees), 0));
        return new Transform3D(basis, Room.Spawn.Position.ToGodot() + new Vector3(0, 1.6f, 0));
    }
}
