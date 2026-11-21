namespace GalaxyCheck.Tests.Slice;

public class ScaffoldTests
{
    [Xunit.Fact]
    public void scaffold_finds_the_repository_root() =>
        Xunit.Assert.True(File.Exists(Path.Combine(Repo.Root, "GalaxyCheck.sln")));
}
