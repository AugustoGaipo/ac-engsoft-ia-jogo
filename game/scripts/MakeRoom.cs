using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;
using Swapping.Levels.Model;

namespace Swapping.Game;

/// <summary>
/// The Godot side of <see cref="FreeSpotSearch"/>: "is this body free with its center here" is a shape
/// query, and "no wall in between" is a ray against the scenery. Bodies ignore the player in their
/// queries (the player's ghost at its old place, see <see cref="Body.GhostSeconds"/>).
/// </summary>
public static class MakeRoom
{
    /// <summary>Shapes are shrunk a little so resting on a floor or touching a wall is not "inside".</summary>
    private const float Margin = 0.04f;

    /// <param name="ignore">The other bodies of the same swap: for a moment they may still be at their old place.</param>
    public static FreeSpot ForPlayer(Player player, Vector3 center, IEnumerable<Body>? ignore = null)
    {
        var size = SpawnDef.PlayerSize.ToGodot();
        var exclude = new Godot.Collections.Array<Rid> { player.GetRid() };
        foreach (var body in ignore ?? [])
            exclude.Add(body.GetRid());
        var capsule = new CapsuleShape3D { Radius = size.X / 2 - Margin / 2, Height = size.Y - Margin };
        return Search(player, capsule, center, isPlayer: true,
            Layers.World | Layers.Objects | Layers.BarrierPlayer, exclude);
    }

    public static FreeSpot ForBody(Body body, Player player, Vector3 center)
    {
        var shape = body.Def.Kind == ObjectKind.Ball
            ? (Shape3D)new SphereShape3D { Radius = (float)body.Def.Size.X / 2 - Margin / 2 }
            : new BoxShape3D { Size = body.Def.Size.ToGodot() - new Vector3(Margin, Margin, Margin) };
        return Search(body, shape, center, isPlayer: false,
            Layers.World | Layers.Objects | Layers.BarrierObjects, [body.GetRid(), player.GetRid()]);
    }

    private static FreeSpot Search(Node3D node, Shape3D shape, Vector3 center, bool isPlayer, uint mask, Godot.Collections.Array<Rid> exclude)
    {
        var space = node.GetWorld3D().DirectSpaceState;
        bool IsFree(Swapping.Rules.Model.Vec3 point) =>
            space.IntersectShape(new PhysicsShapeQueryParameters3D
            {
                Shape = shape,
                Transform = new Transform3D(node.GlobalBasis, point.ToGodot()),
                CollisionMask = mask,
                Exclude = exclude,
            }, 1).Count == 0;
        bool Reachable(Swapping.Rules.Model.Vec3 from, Swapping.Rules.Model.Vec3 to) =>
            space.IntersectRay(PhysicsRayQueryParameters3D.Create(from.ToGodot(), to.ToGodot(), Layers.World, exclude)).Count == 0;

        return FreeSpotSearch.Find(center.ToLevel(), isPlayer, IsFree, Reachable);
    }
}
