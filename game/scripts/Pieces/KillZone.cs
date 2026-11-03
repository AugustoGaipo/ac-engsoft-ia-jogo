using Godot;
using Swapping.Levels.Model;

namespace Swapping.Game.Pieces;

/// <summary>A volume under a pit: whatever enters it dies (player) or pops (object). The reaction comes in Task 8.</summary>
public partial class KillZone : Area3D
{
    public KillZoneDef Def { get; private set; } = null!;

    public static KillZone Create(KillZoneDef def)
    {
        var zone = new KillZone
        {
            Name = def.Id,
            Def = def,
            Position = def.Box.Center(),
            CollisionLayer = 0,
            CollisionMask = Layers.Player | Layers.Objects,
            Monitorable = false,
        };
        zone.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = def.Box.Size() } });
        return zone;
    }
}
