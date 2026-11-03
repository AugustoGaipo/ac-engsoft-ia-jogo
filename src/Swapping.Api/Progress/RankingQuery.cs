using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Swapping.Api.Progress;

/// <summary>
/// Reads <c>mode</c> and <c>top</c> of the ranking routes (P4, B10). Only exact values are accepted:
/// <c>time</c> or <c>moves</c>, and <c>top</c> as plain digits 1–100. A repeated parameter is refused.
/// Anything else is <c>400 InvalidQuery</c>.
/// </summary>
public static class RankingQuery
{
    public const int DefaultTop = 20;
    public const int MaxTop = 100;

    public static string Name(RankingMode mode) => mode switch
    {
        RankingMode.Time => "time",
        RankingMode.Moves => "moves",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    public static bool TryMode(IQueryCollection query, out RankingMode mode)
    {
        mode = default;
        if (!TrySingle(query, "mode", out var text))
            return false;
        switch (text)
        {
            case "time":
                mode = RankingMode.Time;
                return true;
            case "moves":
                mode = RankingMode.Moves;
                return true;
            default:
                return false;
        }
    }

    public static bool TryTop(IQueryCollection query, out int top)
    {
        top = DefaultTop;
        if (!query.ContainsKey("top"))
            return true;
        return TrySingle(query, "top", out var text) &&
               int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out top) &&
               top is >= 1 and <= MaxTop;
    }

    private static bool TrySingle(IQueryCollection query, string key, out string text)
    {
        StringValues values = query[key];
        text = values.Count == 1 ? values[0] ?? "" : "";
        return values.Count == 1;
    }
}
