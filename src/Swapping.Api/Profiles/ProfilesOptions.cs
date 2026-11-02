namespace Swapping.Api.Profiles;

/// <summary>Numbers of the <c>profiles</c> module, from the configuration section <c>Profiles</c>.</summary>
public sealed class ProfilesOptions
{
    public const string Section = "Profiles";

    public static readonly IReadOnlyList<string> DefaultIcons =
        Enumerable.Range(1, 12).Select(i => $"icon-{i:00}").ToArray();

    /// <summary>Icon ids (A2). Empty in configuration → <see cref="DefaultIcons"/>.</summary>
    public string[]? Icons { get; set; }

    public int MaxFailures { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int TokenLifetimeDays { get; set; } = 30;

    public IReadOnlyList<string> IconList => Icons is { Length: > 0 } ? Icons : DefaultIcons;

    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(LockoutMinutes);

    public TimeSpan TokenLifetime => TimeSpan.FromDays(TokenLifetimeDays);
}
