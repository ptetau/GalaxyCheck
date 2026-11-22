namespace S10Dynamic;

/// <summary>Counts how many times a test function runs. The run of the test sets S10_COUNT_FILE.</summary>
public static class Counter
{
    public static void Hit()
    {
        var path = Environment.GetEnvironmentVariable("S10_COUNT_FILE");
        if (!string.IsNullOrEmpty(path))
        {
            File.AppendAllText(path, "x" + Environment.NewLine);
        }
    }
}
