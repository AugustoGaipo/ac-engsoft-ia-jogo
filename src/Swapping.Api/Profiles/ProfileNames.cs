namespace Swapping.Api.Profiles;

/// <summary>
/// Player names (assumption A1): 3–16 ASCII letters, digits, <c>_</c> or <c>-</c>, trimmed, unique
/// ignoring case. No accents, spaces or markup characters, so names are safe in the ranking and in the
/// game UI (S8, S9).
/// </summary>
public static class ProfileNames
{
    public const int MinLength = 3;
    public const int MaxLength = 16;

    public static bool TryNormalize(string? raw, out string name)
    {
        name = raw?.Trim(' ') ?? "";
        return name.Length is >= MinLength and <= MaxLength && name.All(IsAllowed);
    }

    /// <summary>The value that must be unique: the name in uppercase.</summary>
    public static string Key(string name) => name.ToUpperInvariant();

    private static bool IsAllowed(char c) => char.IsAsciiLetterOrDigit(c) || c is '_' or '-';
}
