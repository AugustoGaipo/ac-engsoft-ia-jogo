using System.Globalization;

namespace Swapping.GameLogic;

/// <summary>The time on the HUD: <c>mm:ss.d</c>, tenths cut (never rounded up), minutes past 99 allowed.</summary>
public static class RunClock
{
    public static string Format(int timeMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(timeMs);
        var tenths = timeMs / 100;
        var minutes = tenths / 600;
        var seconds = tenths / 10 % 60;
        return string.Create(CultureInfo.InvariantCulture, $"{minutes:00}:{seconds:00}.{tenths % 10}");
    }
}
