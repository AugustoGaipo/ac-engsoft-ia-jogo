using System.Diagnostics;

namespace Swapping.GameLogic.Tests;

/// <summary>Where the Godot console executable is: <c>GODOT</c> first, then the WinGet install folder.</summary>
public static class GodotPath
{
    public static string? Find()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("GODOT");
        if (!string.IsNullOrEmpty(fromEnvironment) && File.Exists(fromEnvironment))
            return fromEnvironment;

        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA") is { Length: > 0 } fromVariable
            ? fromVariable
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var packages = Path.Combine(localAppData, "Microsoft", "WinGet", "Packages");
        if (!Directory.Exists(packages))
            return null;
        return Directory.EnumerateDirectories(packages, "GodotEngine.GodotEngine.Mono*")
            .SelectMany(d => Directory.EnumerateFiles(d, "Godot_v4.7*_mono_*_console.exe", SearchOption.AllDirectories))
            .OrderDescending(StringComparer.Ordinal)
            .FirstOrDefault();
    }
}

/// <summary>A test that needs Godot: skipped with a message on a machine without it (e.g. in the cloud).</summary>
public sealed class GodotFactAttribute : FactAttribute
{
    public GodotFactAttribute()
    {
        if (GodotPath.Find() is null)
            Skip = "Godot 4.7 .NET was not found (set GODOT to the console exe to run this test).";
    }
}

/// <summary>Runs the real game headless with <c>--smoke-test</c> (SC 10).</summary>
public sealed class GodotSmokeTests
{
    private static string GameDirectory
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Swapping.sln")))
                dir = dir.Parent;
            return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("Swapping.sln not found."), "game");
        }
    }

    public static (int ExitCode, string Output) RunGame(params string[] gameArguments)
    {
        var start = new ProcessStartInfo(GodotPath.Find()!)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[] { "--headless", "--path", GameDirectory, "--" }.Concat(gameArguments))
            start.ArgumentList.Add(argument);

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromMinutes(4)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The game did not quit within 4 minutes.");
        }
        return (process.ExitCode, output.Result + errors.Result);
    }

    [GodotFact]
    public void The_smoke_test_of_level_1_passes()
    {
        var (exitCode, output) = RunGame("--smoke-test", "level-1");

        Assert.True(exitCode == 0, $"exit code {exitCode}{Environment.NewLine}{output}");
        Assert.Contains("[smoke] PASSED", output);
    }

    [GodotFact]
    public void The_menu_smoke_test_passes()
    {
        var (exitCode, output) = RunGame("--smoke-menu");

        Assert.True(exitCode == 0, $"exit code {exitCode}{Environment.NewLine}{output}");
        Assert.Contains("[smoke] PASSED", output);
    }

    [GodotFact]
    public void A_failing_check_exits_1_and_names_the_check()
    {
        var (exitCode, output) = RunGame("--smoke-test", "level-404");

        Assert.Equal(1, exitCode);
        Assert.Contains("[smoke] FAIL level level-404 is in the catalog", output);
    }
}
