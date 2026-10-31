namespace Swapping.Rules.Model;

public enum ContainerType { Player, Object }

/// <summary>
/// The player or an object. Volume never changes. <paramref name="AcceptedProperties"/> null means
/// "accepts every property".
/// </summary>
public sealed record Container(
    string Id,
    ContainerType Type,
    double VolumeM3,
    double ContactAreaM2,
    bool Locked = false,
    IReadOnlySet<IPropertyKind>? AcceptedProperties = null)
{
    public bool Accepts(IPropertyKind kind) => AcceptedProperties is null || AcceptedProperties.Contains(kind);
}
