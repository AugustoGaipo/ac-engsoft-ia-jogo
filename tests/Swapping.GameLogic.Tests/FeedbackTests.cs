namespace Swapping.GameLogic.Tests;

public sealed class FeedbackTests
{
    [Fact]
    public void The_jump_sounds_lower_when_heavy_and_higher_when_light()
    {
        Assert.Equal(1.0, Feedback.JumpPitch(70), 3);
        Assert.True(Feedback.JumpPitch(300) < 0.75);
        Assert.True(Feedback.JumpPitch(2) > 2.0);
        Assert.InRange(Feedback.JumpPitch(0.01), 0.5, 2.5);
        Assert.InRange(Feedback.JumpPitch(100_000), 0.5, 2.5);
    }

    [Theory]
    [InlineData(70, 3.1, 0.0)]      // an ordinary 0.5 m jump: no shake
    [InlineData(2, 12, 0.0)]        // a 2 kg body lands softly even fast
    [InlineData(300, 1.51, 0.27)]   // a 300 kg jump (12 cm): the floor shakes a little
    [InlineData(70, 11.7, 0.76)]    // a 7 m fall
    [InlineData(300, 13, 1.0)]      // capped
    public void Landing_shake_grows_with_momentum(double massKg, double speed, double shake)
    {
        Assert.Equal(shake, Feedback.LandingShake(massKg, speed), 2);
    }

    [Theory]
    [InlineData("queda a 13,2 m/s", "VOCÊ MORREU\nQueda a 13,2 m/s")]
    [InlineData("zona de morte", "VOCÊ MORREU\nZona de morte")]
    [InlineData("", "VOCÊ MORREU")]
    public void The_death_screen_says_why(string cause, string text)
    {
        Assert.Equal(text, Feedback.DeathText(cause));
    }

    [Theory]
    [InlineData(10, 0.8, null)]       // resting or sliding: silent
    [InlineData(10, 5, -9.0)]         // a 10 kg box landing at 5 m/s
    [InlineData(300, 3, 0.0)]         // heavy: loud, capped
    [InlineData(2, 3, -21.0)]         // a light ball: soft
    public void An_impact_sounds_louder_with_more_momentum(double massKg, double speed, double? volumeDb)
    {
        var volume = Feedback.ImpactVolumeDb(massKg, speed);
        if (volumeDb is null) Assert.Null(volume);
        else Assert.Equal(volumeDb.Value, volume!.Value, 0);
    }

    [Fact]
    public void Heavier_objects_sound_lower()
    {
        Assert.Equal(1.0, Feedback.ImpactPitch(10), 3);
        Assert.True(Feedback.ImpactPitch(300) < Feedback.ImpactPitch(10));
        Assert.True(Feedback.ImpactPitch(2) > Feedback.ImpactPitch(10));
    }

    [Theory]
    [InlineData(0.00, 0.05, ChargeSound.None)]
    [InlineData(0.08, 0.11, ChargeSound.Tick)]   // crossed 10 %
    [InlineData(0.55, 0.62, ChargeSound.Tick)]
    [InlineData(0.98, 1.00, ChargeSound.Full)]   // reached the top: the "full" sound, not a tick
    [InlineData(1.00, 1.00, ChargeSound.None)]   // stays full: once only
    public void The_charge_bar_ticks_every_10_percent_and_rings_once_when_full(double before, double after, ChargeSound sound)
    {
        Assert.Equal(sound, Feedback.ChargeStep(before, after));
    }

    [Fact]
    public void The_tick_rises_with_the_bar_and_the_throw_whoosh_with_the_speed()
    {
        Assert.Equal(1.0, Feedback.TickPitch(0), 3);
        Assert.Equal(2.0, Feedback.TickPitch(1), 3);
        Assert.True(Feedback.ThrowPitch(15) > Feedback.ThrowPitch(3));
        Assert.InRange(Feedback.ThrowPitch(100), 0.5, 2.5);
    }

    [Fact]
    public void Each_property_has_its_own_swap_tone()
    {
        var position = Feedback.SwapPitch(Rules.Model.PropertyKind.Position);
        var mass = Feedback.SwapPitch(Rules.Model.PropertyKind.Mass);
        var energy = Feedback.SwapPitch(Rules.Model.PropertyKind.KineticEnergy);
        Assert.Equal(1.0, position, 3);
        Assert.True(mass < position && energy > position, $"{mass} {position} {energy}");
    }
}
