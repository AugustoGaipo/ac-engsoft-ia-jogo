namespace Swapping.Api.Tests;

public static class RepoPaths
{
    public static string Root
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Swapping.sln")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Swapping.sln not found.");
        }
    }
}
