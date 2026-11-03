namespace Swapping.Rules.Model;

/// <summary>
/// The basic, combinable barrier filters. A wall is a thin volume, a zone a big one; the game
/// detects crossings and asks the filter what it blocks. Blocking variables means "all properties" for now.
/// <see cref="RestoresVariables"/> (user's request, 2026-11-03) also blocks swaps and, on contact, gives every
/// variable back to its owner, exchanging with whoever holds it.
/// </summary>
public readonly record struct BarrierFilter(
    bool BlocksObjects = false,
    bool BlocksPlayer = false,
    bool BlocksVariables = false,
    bool RestoresVariables = false)
{
    /// <summary>The exit grid of every level: blocks objects and variables, lets the player pass.</summary>
    public static readonly BarrierFilter ExitGrid = new(BlocksObjects: true, BlocksVariables: true);

    /// <summary>A pure variable barrier: anything passes, no swap through it or touching it.</summary>
    public static readonly BarrierFilter BlocksVariablesOnly = new(BlocksVariables: true);

    /// <summary>A restoring barrier: anything passes; touching it restores every variable (and blocks swaps).</summary>
    public static readonly BarrierFilter Restoring = new(BlocksVariables: true, RestoresVariables: true);

    /// <summary>Whether the container cannot pass through this barrier.</summary>
    public bool BlocksPassageOf(Container container) =>
        container.Type == ContainerType.Player ? BlocksPlayer : BlocksObjects;

    /// <summary>Entering (or touching) restores all variables of the container.</summary>
    public bool RestoresOnEntry => RestoresVariables;

    /// <summary>No swap through it, inside it or touching it.</summary>
    public bool BlocksSwaps => BlocksVariables || RestoresVariables;

    public BarrierFilter Combine(BarrierFilter other) => new(
        BlocksObjects || other.BlocksObjects,
        BlocksPlayer || other.BlocksPlayer,
        BlocksVariables || other.BlocksVariables,
        RestoresVariables || other.RestoresVariables);
}
