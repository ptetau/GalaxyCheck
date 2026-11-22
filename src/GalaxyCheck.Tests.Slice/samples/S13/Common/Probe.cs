using System.Globalization;

namespace S13Sample;

/// <summary>Helpers of the S13 sample projects: a log of the inputs that each test function receives.</summary>
public static class Probe
{
    public static string ProjectDir { get; } = Find();

    public static bool Hold => File.Exists(Path.Combine(ProjectDir, "hold.txt"));

    public static void Log(string test, object value)
    {
        var folder = Path.Combine(ProjectDir, "logs");
        Directory.CreateDirectory(folder);
        var line = Convert.ToString(value, CultureInfo.InvariantCulture) + "\n";
        lock (typeof(Probe))
        {
            File.AppendAllText(Path.Combine(folder, test + ".txt"), line);
        }
    }

    private static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && directory.GetFiles("*.csproj").Length == 0)
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The project folder was not found");
    }
}
