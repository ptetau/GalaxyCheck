using System.Text.Json;
using GalaxyCheck.Configuration;

// The library asks the test project how to compare a snapshot. This store keeps each snapshot in a file
// next to the project: the first run writes it, and a later run compares with it.
public class SnapshotStore : IConfigureGlobal
{
    public void Configure(IGlobalConfiguration instance)
    {
        instance.GenSnapshots.AssertSnapshotMatches = snapshot =>
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && directory.GetFiles("*.csproj").Length == 0)
            {
                directory = directory.Parent;
            }

            var folder = Path.Combine(directory!.FullName, "__snapshots__");
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, $"{snapshot.TestClassName}.{snapshot.TestMethodName}.{snapshot.Seed}.json");
            var text = JsonSerializer.Serialize(snapshot.Output);
            if (!File.Exists(file))
            {
                File.WriteAllText(file, text);
                return Task.CompletedTask;
            }

            var stored = File.ReadAllText(file);
            if (stored != text)
            {
                throw new Exception($"The snapshot changed. Stored: {stored}. Now: {text}.");
            }

            return Task.CompletedTask;
        };
    }
}
