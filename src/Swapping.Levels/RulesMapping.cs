using Swapping.Levels.Model;
using Swapping.Rules;
using Swapping.Rules.Model;
using Swapping.Rules.Policies;

namespace Swapping.Levels;

/// <summary>
/// Turns a (valid) level into <c>rules</c> types. Every call builds new objects, so building a room
/// again is how a room restarts. Unknown room ids are programming errors and throw.
/// </summary>
public static class RulesMapping
{
    /// <summary>The player's body is a 0.6 × 1.8 × 0.6 m column; its position in the world is its center.</summary>
    public static readonly Vec3 PlayerCenterAboveFeet = new(0, SpawnDef.PlayerSize.Y / 2, 0);

    public static readonly double PlayerContactAreaM2 = SpawnDef.PlayerSize.X * SpawnDef.PlayerSize.Z;

    public static IPropertyKind ToPropertyKind(string name) => name switch
    {
        PropertyNames.Position => PropertyKind.Position,
        PropertyNames.Mass => PropertyKind.Mass,
        PropertyNames.KineticEnergy => PropertyKind.KineticEnergy,
        _ => throw new ArgumentException($"Unknown property '{name}'.", nameof(name)),
    };

    public static LevelRules ToLevelRules(LevelFile level, string roomId)
    {
        var room = Room(level, roomId);
        var over = room.RulesOverride ?? new RulesOverrideDef();
        var rules = level.Rules;

        return new LevelRules
        {
            UnlockedProperties = (over.UnlockedProperties ?? rules.UnlockedProperties).Select(ToPropertyKind).ToHashSet(),
            RangeMeters = (over.RangeMeters ?? rules.RangeMeters).ToDictionary(p => ToPropertyKind(p.Key), p => p.Value),
            CooldownSeconds = over.CooldownSeconds ?? rules.CooldownSeconds,
            AllowObjectToObject = over.AllowObjectToObject ?? rules.AllowObjectToObject,
            AllowSimultaneousSwaps = over.AllowSimultaneousSwaps ?? rules.AllowSimultaneousSwaps,
        };
    }

    /// <summary>A new world for the room: the player at the spawn, at rest, and every object at its start values.</summary>
    public static World BuildWorld(LevelFile level, string roomId)
    {
        var room = Room(level, roomId);
        var world = new World(ToLevelRules(level, roomId));

        world.Add(
            new Container(LevelValidator.PlayerId, ContainerType.Player, LevelFile.PlayerVolumeM3, PlayerContactAreaM2),
            room.Spawn.Position + PlayerCenterAboveFeet,
            LevelFile.PlayerMassKg,
            Vec3.Zero);

        foreach (var obj in room.Objects)
        {
            var accepted = obj.Accepts?.Select(ToPropertyKind).ToHashSet();
            world.Add(
                new Container(obj.Id, ContainerType.Object, obj.VolumeM3, obj.ContactAreaM2, obj.Locked, accepted),
                obj.Position,
                obj.MassKg,
                obj.Velocity);
        }

        return world;
    }

    private static RoomDef Room(LevelFile level, string roomId) =>
        level.Rooms.FirstOrDefault(r => r.Id == roomId)
        ?? throw new KeyNotFoundException($"Level '{level.Id}' has no room '{roomId}'.");
}
