using Swapping.Levels.Model;

namespace Swapping.Levels;

public static partial class LevelValidator
{
    /// <summary>
    /// Checks that compare pieces with each other. Boxes already reported as empty are skipped, and a
    /// room with empty bounds skips every spatial check (they would all be noise).
    /// </summary>
    private static void CheckSpatial(LevelFile level, List<LevelError> errors)
    {
        void Add(string path, LevelErrorCode code, string message) => errors.Add(new LevelError(path, code, message));

        for (var r = 0; r < level.Rooms.Count; r++)
        {
            var room = level.Rooms[r];
            var path = $"rooms[{Inv(r)}]";
            if (room.Bounds.IsEmpty)
                continue;

            for (var earlier = 0; earlier < r; earlier++)
                if (!level.Rooms[earlier].Bounds.IsEmpty && room.Bounds.Overlaps(level.Rooms[earlier].Bounds))
                    Add($"{path}.bounds", LevelErrorCode.RoomsOverlap, $"Room {room.Id} overlaps room {level.Rooms[earlier].Id}.");

            CheckInside(room, path, Add);
            CheckLinks(room, path, Add);
            CheckExitGrid(room, path, Add);
            CheckOverlaps(room, path, Add);
            CheckSpawn(room, path, Add);
        }
    }

    private static void CheckInside(RoomDef room, string path, Action<string, LevelErrorCode, string> add)
    {
        void Inside(Box box, string p, string what)
        {
            if (!box.IsEmpty && !room.Bounds.Contains(box))
                add(p, LevelErrorCode.OutsideRoom, $"{what} is not fully inside room {room.Id}.");
        }

        Inside(room.Spawn.Area, $"{path}.spawn", "The spawn (player-sized area)");
        Inside(room.Exit, $"{path}.exit", "The exit");
        for (var i = 0; i < room.Objects.Count; i++)
            Inside(room.Objects[i].Bounds, $"{path}.objects[{Inv(i)}]", $"Object '{room.Objects[i].Id}'");
        for (var i = 0; i < room.Buttons.Count; i++)
            Inside(room.Buttons[i].Bounds, $"{path}.buttons[{Inv(i)}]", $"Button '{room.Buttons[i].Id}'");
        for (var i = 0; i < room.Doors.Count; i++)
            Inside(room.Doors[i].Box, $"{path}.doors[{Inv(i)}]", $"Door '{room.Doors[i].Id}'");
        for (var i = 0; i < room.Barriers.Count; i++)
            Inside(room.Barriers[i].Box, $"{path}.barriers[{Inv(i)}]", $"Barrier '{room.Barriers[i].Id}'");
        for (var i = 0; i < room.KillZones.Count; i++)
            Inside(room.KillZones[i].Box, $"{path}.killZones[{Inv(i)}]", $"Kill zone '{room.KillZones[i].Id}'");
    }

    private static void CheckLinks(RoomDef room, string path, Action<string, LevelErrorCode, string> add)
    {
        var buttons = room.Buttons.Select(b => b.Id).ToHashSet(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < room.Doors.Count; i++)
        {
            var door = room.Doors[i];
            var p = $"{path}.doors[{Inv(i)}].opensWith";
            if (door.OpensWith.Count == 0)
                add(p, LevelErrorCode.DoorWithoutButton, $"Door '{door.Id}' has no button to open it.");

            for (var j = 0; j < door.OpensWith.Count; j++)
            {
                var id = door.OpensWith[j];
                if (buttons.Contains(id))
                    used.Add(id);
                else
                    add($"{p}[{Inv(j)}]", LevelErrorCode.UnknownReference, $"'{id}' is not a button of room {room.Id}.");
            }
        }

        for (var i = 0; i < room.Buttons.Count; i++)
            if (!used.Contains(room.Buttons[i].Id))
                add($"{path}.buttons[{Inv(i)}]", LevelErrorCode.UnusedButton, $"No door uses button '{room.Buttons[i].Id}'.");
    }

    private static void CheckExitGrid(RoomDef room, string path, Action<string, LevelErrorCode, string> add)
    {
        if (room.Exit.IsEmpty)
            return;
        if (!room.Barriers.Any(b => b.IsExitGrid && !b.Box.IsEmpty && b.Box.Overlaps(room.Exit)))
            add($"{path}.exit", LevelErrorCode.MissingExitGrid,
                "The exit must be covered by an exit grid (blocks objects and variables, lets the player pass).");
    }

    private static void CheckOverlaps(RoomDef room, string path, Action<string, LevelErrorCode, string> add)
    {
        var spawn = room.Spawn.Area;
        for (var i = 0; i < room.Objects.Count; i++)
        {
            var obj = room.Objects[i];
            var box = obj.Bounds;
            if (box.IsEmpty)
                continue;

            string? with = null;
            for (var k = 0; k < i && with is null; k++)
                if (!room.Objects[k].Bounds.IsEmpty && box.Overlaps(room.Objects[k].Bounds))
                    with = $"object '{room.Objects[k].Id}'";
            with ??= room.Buttons.FirstOrDefault(b => !b.Bounds.IsEmpty && box.Overlaps(b.Bounds)) is { } button ? $"button '{button.Id}'" : null;
            with ??= room.Doors.FirstOrDefault(d => !d.Box.IsEmpty && box.Overlaps(d.Box)) is { } door ? $"door '{door.Id}'" : null;
            with ??= room.KillZones.FirstOrDefault(z => !z.Box.IsEmpty && box.Overlaps(z.Box)) is { } zone ? $"kill zone '{zone.Id}'" : null;
            with ??= box.Overlaps(spawn) ? "the spawn area" : null;

            if (with is not null)
                add($"{path}.objects[{Inv(i)}]", LevelErrorCode.Overlap, $"Object '{obj.Id}' starts inside {with}.");
        }
    }

    private static void CheckSpawn(RoomDef room, string path, Action<string, LevelErrorCode, string> add)
    {
        var spawn = room.Spawn.Area;
        if (room.KillZones.Any(z => !z.Box.IsEmpty && spawn.Overlaps(z.Box)))
            add($"{path}.spawn", LevelErrorCode.SpawnUnsafe, "The player would spawn inside a kill zone.");
        else if (room.Barriers.Any(b => b.Filter.BlocksPlayer && !b.Box.IsEmpty && spawn.Overlaps(b.Box)))
            add($"{path}.spawn", LevelErrorCode.SpawnUnsafe, "The player would spawn inside a barrier that blocks the player.");
    }
}
