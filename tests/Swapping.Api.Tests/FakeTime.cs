namespace Swapping.Api.Tests;

/// <summary>A clock the tests move by hand (lockout, token expiry).</summary>
public sealed class FakeTime(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset now = start;

    public override DateTimeOffset GetUtcNow() => now;

    public void Advance(TimeSpan by) => now += by;
}
