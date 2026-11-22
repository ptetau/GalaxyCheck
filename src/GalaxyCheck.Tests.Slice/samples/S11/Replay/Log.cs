namespace S11.Replay;

/// <summary>Writes one line for each call of a test function, to a file for each test. The test reads the files.</summary>
public static class Log
{
    private static readonly object Gate = new();

    public static void Write(string test, string line)
    {
        var folder = Environment.GetEnvironmentVariable("GC_LOG_DIR");
        if (folder is null)
        {
            return;
        }

        lock (Gate)
        {
            File.AppendAllText(Path.Combine(folder, test + ".log"), line + "\n");
        }
    }
}
