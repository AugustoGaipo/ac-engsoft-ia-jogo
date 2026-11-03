using System.Globalization;
using Swapping.Levels.Model;
using Swapping.Rules.Model;
using Swapping.Rules.Operations;
using Swapping.Rules.Physics;

namespace Swapping.GameLogic;

/// <summary>Idle: a small dot always on screen (user's request); Ready: white +; Refused: red + with the reason.</summary>
public enum CrosshairLook { Idle, Ready, Refused }

/// <summary>What the crosshair shows: white (ready), red with a short reason, or the idle dot.</summary>
public sealed record Crosshair(CrosshairLook Look, string Reason);

/// <summary>The crosshair from the dry run of the rules (<c>LevelRules.Check</c>) on the aimed body.</summary>
public static class CrosshairState
{
    public static Crosshair For(bool aimingAtContainer, RejectReason? reason) =>
        !aimingAtContainer ? new Crosshair(CrosshairLook.Idle, "")
        : reason is not { } why ? new Crosshair(CrosshairLook.Ready, "")
        : new Crosshair(CrosshairLook.Refused, Text(why));

    private static string Text(RejectReason reason) => reason switch
    {
        RejectReason.SameContainer => "você mesmo",
        RejectReason.ModeNotEnabled => "objeto com objeto: ainda não",
        RejectReason.PropertyNotUnlocked => "trocas bloqueadas aqui",
        RejectReason.PropertyNotAccepted => "não aceita esta propriedade",
        RejectReason.Locked => "travado",
        RejectReason.OutOfRange => "fora de alcance",
        RejectReason.NoLineOfSight => "sem linha de visão",
        RejectReason.BlockedByBarrier => "barreira no caminho",
        RejectReason.InsideVariableBarrier => "dentro de uma barreira",
        RejectReason.OnCooldown => "recarregando",
        RejectReason.TooManyProperties => "uma propriedade por vez",
        _ => throw new ArgumentOutOfRangeException(nameof(reason)),
    };
}

/// <summary>Which property the swap button asks for in a room (only one at a time in this slice).</summary>
public static class SwapChoice
{
    private static readonly IPropertyKind[] Order = [PropertyKind.Position, PropertyKind.Mass, PropertyKind.KineticEnergy];

    /// <summary>The first unlocked one in a fixed order; with nothing unlocked, position (the rules then refuse it).</summary>
    public static IPropertyKind Property(IReadOnlySet<IPropertyKind> unlocked) =>
        Order.FirstOrDefault(unlocked.Contains) ?? PropertyKind.Position;
}

/// <summary>The numbers of a body on the HUD: mass, speed and kinetic energy (Brazilian decimal comma).</summary>
public sealed record Readout(string Mass, string Speed, string Energy)
{
    public static Readout Of(double massKg, Vec3 velocity)
    {
        var speed = velocity.Length;
        return new Readout(
            Number(massKg, "0.##") + " kg",
            Number(speed, "0.0") + " m/s",
            Number(Formulas.KineticEnergy(massKg, speed), "0") + " J");
    }

    // Invariant format with the comma swapped in: the engine may run with invariant globalization.
    private static string Number(double value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture).Replace('.', ',');
}

/// <summary>Barriers that block variables (barrier 3, the exit grid): crossed by a segment, or containing a point.</summary>
public static class BarrierGeometry
{
    /// <summary>Whether the segment touches the box (slab test, faces included).</summary>
    public static bool Crosses(Vec3 from, Vec3 to, Box box)
    {
        double enter = 0, leave = 1;
        return Slab(from.X, to.X, box.Min.X, box.Max.X, ref enter, ref leave)
            && Slab(from.Y, to.Y, box.Min.Y, box.Max.Y, ref enter, ref leave)
            && Slab(from.Z, to.Z, box.Min.Z, box.Max.Z, ref enter, ref leave);
    }

    public static bool Inside(Vec3 point, Box box) => box.Contains(point);

    private static bool Slab(double from, double to, double min, double max, ref double enter, ref double leave)
    {
        var delta = to - from;
        if (Math.Abs(delta) < 1e-12)
            return from >= min && from <= max;
        var t1 = (min - from) / delta;
        var t2 = (max - from) / delta;
        if (t1 > t2)
            (t1, t2) = (t2, t1);
        enter = Math.Max(enter, t1);
        leave = Math.Min(leave, t2);
        return enter <= leave;
    }
}
