namespace Swapping.Rules.Model;

/// <summary>
/// The three basic, combinable barrier filters. A wall is a thin volume, a zone a big one; the game
/// detects crossings and asks the filter what it blocks. Blocking variables means "all properties" for now.
/// </summary>
public readonly record struct BarrierFilter(
    bool BlocksObjects = false,
    bool BlocksPlayer = false,
    bool BlocksVariables = false)
{
    /// <summary>The exit grid of every level: blocks objects and variables, lets the player pass.</summary>
    public static readonly BarrierFilter ExitGrid = new(BlocksObjects: true, BlocksVariables: true);

    /// <summary>A pure variable barrier: anything passes, but entering restores every variable.</summary>
    public static readonly BarrierFilter BlocksVariablesOnly = new(BlocksVariables: true);

    /// <summary>Whether the container cannot pass through this barrier.</summary>
    public bool BlocksPassageOf(Container container) =>
        container.Type == ContainerType.Player ? BlocksPlayer : BlocksObjects;

    /// <summary>Entering restores all variables of the container.</summary>
    public bool RestoresOnEntry => BlocksVariables;

    /// <summary>No swap through it or inside it.</summary>
    public bool BlocksSwaps => BlocksVariables;

    public BarrierFilter Combine(BarrierFilter other) => new(
        BlocksObjects || other.BlocksObjects,
        BlocksPlayer || other.BlocksPlayer,
        BlocksVariables || other.BlocksVariables);
}
