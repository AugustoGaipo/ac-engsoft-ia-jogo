namespace Swapping.GameLogic;

public enum GameAction
{
    MoveForward,
    MoveBack,
    MoveLeft,
    MoveRight,
    Jump,
    /// <summary>Left button (user, 2026-11-05): grabs when the hands are empty; holding it charges a throw, a tap drops.</summary>
    Throw,
    Swap,
    RestartRoom,
    Pause,
    ChoosePosition,
    ChooseMass,
    ChooseEnergy,
    PropertyPrevious,
    PropertyNext,
    HoldToRotate,
    Sprint,
}

public enum InputKind { Key, MouseButton }

/// <summary>One control: an action and the key or mouse button for it (the engine's own names).</summary>
public sealed record InputBinding(GameAction Action, InputKind Kind, string Code);

/// <summary>The controls of decision G1. The game registers these in the engine at start.</summary>
public static class Controls
{
    public static IReadOnlyList<InputBinding> Bindings { get; } =
    [
        new(GameAction.MoveForward, InputKind.Key, "W"),
        new(GameAction.MoveBack, InputKind.Key, "S"),
        new(GameAction.MoveLeft, InputKind.Key, "A"),
        new(GameAction.MoveRight, InputKind.Key, "D"),
        new(GameAction.Jump, InputKind.Key, "Space"),
        new(GameAction.Throw, InputKind.MouseButton, "Left"),
        new(GameAction.Swap, InputKind.MouseButton, "Right"),
        new(GameAction.RestartRoom, InputKind.Key, "R"),
        new(GameAction.Pause, InputKind.Key, "Escape"),
        new(GameAction.ChoosePosition, InputKind.Key, "Key1"),
        new(GameAction.ChooseMass, InputKind.Key, "Key2"),
        new(GameAction.ChooseEnergy, InputKind.Key, "Key3"),
        new(GameAction.PropertyPrevious, InputKind.MouseButton, "WheelUp"),
        new(GameAction.PropertyNext, InputKind.MouseButton, "WheelDown"),
        new(GameAction.HoldToRotate, InputKind.Key, "F"),
        new(GameAction.Sprint, InputKind.Key, "Shift"),
    ];

    /// <summary>The action's name in the engine: snake case of the enum name (<c>MoveForward</c> → <c>move_forward</c>).</summary>
    public static string Name(GameAction action) =>
        string.Concat(action.ToString().Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
