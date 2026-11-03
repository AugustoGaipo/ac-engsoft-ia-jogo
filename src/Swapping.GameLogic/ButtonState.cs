using Swapping.Levels.Model;

namespace Swapping.GameLogic;

/// <summary>Something resting on a button.</summary>
public readonly record struct WeightOnButton(bool IsPlayer, double MassKg);

/// <summary>Buttons and doors (levels spec): a button is pressed by mass, a door opens with its buttons.</summary>
public static class ButtonState
{
    /// <summary>
    /// Pressed while the mass on it is at least <paramref name="requiredMassKg"/>, counting only what
    /// <paramref name="detects"/> allows (<c>objects</c> ignores the player).
    /// </summary>
    public static bool IsPressed(double requiredMassKg, ButtonDetects detects, IEnumerable<WeightOnButton> onTop) =>
        onTop.Where(w => detects == ButtonDetects.Any || !w.IsPlayer).Sum(w => w.MassKg) >= requiredMassKg;

    /// <summary>Open while every button it lists is pressed. A door with no button never opens.</summary>
    public static bool IsDoorOpen(IReadOnlyCollection<string> opensWith, IReadOnlySet<string> pressedButtons) =>
        opensWith.Count > 0 && opensWith.All(pressedButtons.Contains);
}
