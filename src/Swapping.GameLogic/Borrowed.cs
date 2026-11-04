using Swapping.Rules;
using Swapping.Rules.Model;

namespace Swapping.GameLogic;

/// <summary>
/// Which properties a body uses that belong to someone else (user's request, 2026-11-03): after a swap the HUD
/// shows "300 kg ⇄ da caixa" for the player and "70 kg ⇄ seu" on the aimed object.
/// </summary>
public static class Borrowed
{
    /// <summary>The properties <paramref name="containerId"/> uses that another container owns, with that owner.</summary>
    public static IReadOnlyList<(IPropertyKind Kind, string Owner)> Of(World world, string containerId) =>
        PropertyKind.All
            .Select(kind => (Kind: kind, Owner: world.VariableUsedBy(containerId, kind).Owner))
            .Where(p => p.Owner != containerId)
            .ToList();

    /// <summary>"" when the property is the body's own; " ⇄ seu" when it is the player's; " ⇄ da {name}" otherwise.</summary>
    public static string Mark(World world, string containerId, IPropertyKind kind, string playerId, Func<string, string> nameOf)
    {
        var owner = world.VariableUsedBy(containerId, kind).Owner;
        if (owner == containerId) return "";
        return owner == playerId ? " ⇄ seu" : $" ⇄ da {nameOf(owner)}";
    }
}
