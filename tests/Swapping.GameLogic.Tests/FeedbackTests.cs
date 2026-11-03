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
}
