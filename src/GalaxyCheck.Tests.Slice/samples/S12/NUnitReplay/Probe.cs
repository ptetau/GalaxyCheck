namespace Sample;

/// <summary>Writes one line for each call to a file that the test of the repository reads.</summary>
public static class Probe
{
    public static void Append(string name, string line)
    {
        var folder = Path.Combine(Path.GetTempPath(), "galaxycheck-s12-probe");
        Directory.CreateDirectory(folder);
        File.AppendAllText(Path.Combine(folder, "nunit-" + name + ".txt"), line + "\n");
    }
}
