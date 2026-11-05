using Swapping.Rules;

namespace Swapping.GameLogic;

/// <summary>
/// What a finished run hands to <c>sync</c>: the same fields and names as <c>POST /results</c>.
/// </summary>
public sealed record RunResult(string LevelId, int TimeMs, int Swaps, int Deaths, int RoomRestarts, DateTimeOffset FinishedAt);

/// <summary>
/// The totals of one run of a level (SPEC-game, "Rooms, deaths and the run"): time from the physics
/// steps (stopped while paused), swaps added up over every room build, deaths and room restarts.
/// A room restart keeps the totals; a level restart starts a new run.
/// </summary>
public sealed class RunTracker(string levelId)
{
    private double _seconds;
    private int _swapsOfRetiredWorlds;
    private readonly List<World> _live = [];
    private World? _room;

    public string LevelId { get; } = levelId;

    /// <summary>Whole milliseconds, capped at <see cref="int.MaxValue"/> (about 24 days).</summary>
    public int TimeMs => (int)Math.Min(Math.Floor(_seconds * 1000 + 1e-6), int.MaxValue);

    /// <summary>The swaps of every room world of the run, each counted once, plus those of worlds rebuilt since.</summary>
    public int Swaps => _swapsOfRetiredWorlds + _live.Sum(w => w.MoveCount);

    public int Deaths { get; private set; }

    public int RoomRestarts { get; private set; }

    public bool IsPaused { get; private set; }

    public bool IsFinished { get; private set; }

    /// <summary>One physics step. Ignored while paused or after the run ended.</summary>
    public void Tick(double deltaSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(deltaSeconds);
        if (!IsPaused && !IsFinished)
            _seconds += deltaSeconds;
    }

    public void Pause() => IsPaused = true;

    public void Resume() => IsPaused = false;

    /// <summary>The player is now in this room (first time, or back in it: all rooms stay alive). Counted once.</summary>
    public void EnterRoom(World world)
    {
        if (!_live.Contains(world))
            _live.Add(world);
        _room = world;
    }

    /// <summary>The room was built again (death or menu): the totals stay.</summary>
    public void RestartRoom(World rebuilt)
    {
        if (_room is not null && _live.Remove(_room))
            _swapsOfRetiredWorlds += _room.MoveCount;
        EnterRoom(rebuilt);
        RoomRestarts++;
    }

    public void RecordDeath() => Deaths++;

    /// <summary>Back to the first room: a new run, everything at zero.</summary>
    public void RestartLevel(World firstRoom)
    {
        _seconds = 0;
        _swapsOfRetiredWorlds = 0;
        _live.Clear();
        _live.Add(firstRoom);
        _room = firstRoom;
        Deaths = 0;
        RoomRestarts = 0;
        IsPaused = false;
        IsFinished = false;
    }

    /// <summary>The exit of the last room was crossed: the run stops and its result is returned.</summary>
    public RunResult Finish(DateTimeOffset finishedAt)
    {
        if (IsFinished)
            throw new InvalidOperationException("The run is already finished.");
        IsFinished = true;
        return new RunResult(LevelId, TimeMs, Swaps, Deaths, RoomRestarts, finishedAt);
    }
}
