using Godot;

namespace Swapping.Game;

/// <summary>
/// Sounds made in code (no audio files): short sweeps and noise bursts written into 16-bit mono WAV streams.
/// Built once; the players change pitch and volume per use (Feedback).
/// </summary>
public static class Sfx
{
    private const int Rate = 22050;

    /// <summary>A rising "hop": 260 → 520 Hz in 0.14 s.</summary>
    public static readonly AudioStreamWav Jump = Sweep(260, 520, 0.14, noise: 0.05);

    /// <summary>A low thud: 90 → 45 Hz with noise, 0.3 s.</summary>
    public static readonly AudioStreamWav Land = Sweep(90, 45, 0.3, noise: 0.45);

    /// <summary>A falling tone for the death: 520 → 70 Hz in 0.7 s.</summary>
    public static readonly AudioStreamWav Death = Sweep(520, 70, 0.7, noise: 0.15);

    /// <summary>A frequency sweep with an exponential fade, plus a bit of seeded noise.</summary>
    public static AudioStreamWav Sweep(double fromHz, double toHz, double seconds, double noise)
    {
        var count = (int)(Rate * seconds);
        var data = new byte[count * 2];
        var random = new System.Random(7);
        var phase = 0.0;
        for (var i = 0; i < count; i++)
        {
            var t = (double)i / count;
            phase += 2 * Mathf.Pi * (fromHz + (toHz - fromHz) * t) / Rate;
            var envelope = Mathf.Exp(-4 * t) * Mathf.Min(1, i / (Rate * 0.005));   // 5 ms attack, no click
            var sample = ((1 - noise) * Mathf.Sin(phase) + noise * (random.NextDouble() * 2 - 1)) * envelope;
            var value = (short)(Mathf.Clamp(sample, -1, 1) * 0.8 * short.MaxValue);
            data[2 * i] = (byte)(value & 0xFF);
            data[2 * i + 1] = (byte)((value >> 8) & 0xFF);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = Rate, Stereo = false, Data = data };
    }

    /// <summary>A player for one sound, child of <paramref name="owner"/>.</summary>
    public static AudioStreamPlayer Player(Node owner, AudioStreamWav stream, string name)
    {
        var player = new AudioStreamPlayer { Name = name, Stream = stream, MaxPolyphony = 3 };
        owner.AddChild(player);
        return player;
    }
}
