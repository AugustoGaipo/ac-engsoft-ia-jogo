using Swapping.Rules.Model;
using Swapping.Rules.Physics;

namespace Swapping.Rules.Tests;

public class FormulaTests
{
    private const double Tol = 0.001;

    [Fact]
    public void KineticEnergy_AndSpeedFromEnergy_RoundTrip()
    {
        Assert.Equal(9.0, Formulas.KineticEnergy(2, 3), Tol);
        Assert.Equal(3.0, Formulas.SpeedFromEnergy(9, 2), Tol);
        Assert.Equal(0.0, Formulas.SpeedFromEnergy(0, 5), Tol);
    }

    [Fact] // SC 3
    public void MassChange_KeepsEnergy()
    {
        var energy = Formulas.KineticEnergy(2, 3);
        Assert.Equal(1.5, Formulas.SpeedFromEnergy(energy, 8), Tol);
    }

    [Fact] // SC 4 numbers
    public void EnergyOfBall_GivesPlayerSpeed()
    {
        var energy = Formulas.KineticEnergy(1, 4);
        Assert.Equal(0.478, Formulas.SpeedFromEnergy(energy, 70), Tol);
    }

    [Fact]
    public void Density_IsMassOverVolume() =>
        Assert.Equal(1000.0, Formulas.Density(70, 0.07), Tol);

    [Fact] // SC 10
    public void Pressure_MatchesSpec()
    {
        Assert.Equal(9800.0, Formulas.StaticPressure(10, 0.01), Tol);
        Assert.Equal(980.0, Formulas.StaticPressure(10, 0.1), Tol);
        Assert.True(Formulas.Breaks(9800, 5000));
        Assert.False(Formulas.Breaks(9800, 10000));
    }

    [Fact]
    public void BuoyancyForce_IsUpwardWhenFluidDenser()
    {
        // 0.01 m3 of density 0 in water: 1000 * 0.01 * 9.8
        Assert.Equal(98.0, Formulas.BuoyancyForce(1000, 0, 0.01), Tol);
        Assert.True(Formulas.BuoyancyForce(1000, 2000, 0.01) < 0);
    }

    [Fact] // SC 11
    public void PlayerInWater_Swims()
    {
        var rho = Formulas.Density(70, 0.07);
        Assert.Equal(BuoyancyState.Swim, Formulas.BuoyancyState(rho, Medium.Water.Density));
    }

    [Fact] // SC 11
    public void PlayerHoldingRock_Sinks()
    {
        var rho = Formulas.Density(70 + 30, 0.07 + 0.01);
        Assert.Equal(1250.0, rho, Tol);
        Assert.Equal(BuoyancyState.Sink, Formulas.BuoyancyState(rho, Medium.Water.Density));
    }

    [Fact] // SC 11
    public void LightPlayerInAir_Swims()
    {
        var rho = Formulas.Density(0.05, 0.07);
        Assert.Equal(0.714, rho, Tol);
        Assert.Equal(BuoyancyState.Swim, Formulas.BuoyancyState(rho, Medium.Air.Density));
    }

    [Theory]
    [InlineData(1100.0, BuoyancyState.Swim)]
    [InlineData(100.0, BuoyancyState.Swim)]
    [InlineData(1100.5, BuoyancyState.Sink)]
    [InlineData(99.5, BuoyancyState.Float)]
    public void BuoyancyBands_InWater(double rho, BuoyancyState expected) =>
        Assert.Equal(expected, Formulas.BuoyancyState(rho, 1000));

    [Theory]
    [InlineData(1.32, BuoyancyState.Swim)]
    [InlineData(0.12, BuoyancyState.Swim)]
    [InlineData(1.33, BuoyancyState.Sink)]
    [InlineData(0.11, BuoyancyState.Float)]
    public void BuoyancyBands_InAir(double rho, BuoyancyState expected) =>
        Assert.Equal(expected, Formulas.BuoyancyState(rho, Medium.Air.Density));

    [Fact]
    public void Recoil_OnlyWhenNotOnGround()
    {
        Assert.Equal(1.0, Formulas.RecoilSpeed(0.01, 5, 0.05, onGround: false), Tol);
        Assert.Equal(0.0, Formulas.RecoilSpeed(0.01, 5, 0.05, onGround: true), Tol);
        Assert.Equal(0.0, Formulas.RecoilSpeed(0.01, 0, 0.05, onGround: false), Tol); // dropping
    }

    [Theory] // SC 12 (density 1000 kg/m3: player body, g_eff ~ g)
    [InlineData(70, 0.50)]
    [InlineData(35, 1.00)]
    [InlineData(7, 5.01)]
    [InlineData(300, 0.12)]
    public void JumpHeight_InAir(double mass, double expected)
    {
        var h = Formulas.JumpHeight(mass, density: 1000, fluidDensity: Medium.Air.Density);
        Assert.Equal(expected, h, 0.01);
    }

    [Theory] // SC 12 with the real density of the player: fixed volume 0.07 m3, so rho = m / 0.07
    [InlineData(70, 0.50)]
    [InlineData(35, 1.00)]
    [InlineData(7, 5.06)]
    [InlineData(300, 0.12)]
    public void JumpHeight_InAir_WithPlayerFixedVolume(double mass, double expected)
    {
        var density = Formulas.Density(mass, volumeM3: 0.07);
        var h = Formulas.JumpHeight(mass, density, fluidDensity: Medium.Air.Density);
        Assert.Equal(expected, h, 0.01);
    }

    [Theory] // no cap: the take-off speed is just sqrt(2·343/m), so a light body leaves the ground faster
    [InlineData(7, 9.8995)]
    [InlineData(1, 26.1916)]
    [InlineData(70, 3.1305)]
    public void JumpTakeoffSpeed_HasNoCap(double mass, double expected) =>
        Assert.Equal(expected, Formulas.JumpTakeoffSpeed(mass), 0.001);

    [Fact]
    public void JumpNearMediumDensity_IsMoonLike()
    {
        var normal = Formulas.JumpHeight(70, 1000, 1.2);
        var nearWater = Formulas.JumpHeight(70, 2000, 1000); // g_eff = g/2
        Assert.Equal(normal * 2, nearWater, 0.01);
        Assert.True(double.IsPositiveInfinity(Formulas.JumpHeight(70, 500, 1000)));
    }

    [Fact]
    public void Media_HaveSpecDensities()
    {
        Assert.Equal(1.2, Medium.Air.Density, Tol);
        Assert.Equal(6.1, Medium.DenseGas.Density, Tol);
        Assert.Equal(1000.0, Medium.Water.Density, Tol);
        Assert.True(Medium.Water.Drag > Medium.Air.Drag);
        Assert.Equal(3.0, Formulas.DefaultSwimSpeed, Tol);
    }

    [Fact]
    public void Vec3_Operations()
    {
        var a = new Vec3(1, 2, 2);
        Assert.Equal(3.0, a.Length, Tol);
        Assert.Equal(new Vec3(2, 4, 4), a * 2);
        Assert.Equal(new Vec3(2, 3, 3), a + new Vec3(1, 1, 1));
        Assert.Equal(new Vec3(0, 1, 1), a - new Vec3(1, 1, 1));
        Assert.Equal(new Vec3(1.0 / 3, 2.0 / 3, 2.0 / 3).X, a.Normalized().X, Tol);
        Assert.Equal(Vec3.Zero, Vec3.Zero.Normalized());
    }
}
