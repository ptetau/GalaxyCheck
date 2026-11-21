using GalaxyCheck;
using GalaxyCheck.Gens;
using Xunit;

namespace S10Sample;

public static class Rec
{
    /// <summary>Appends one line to the file that the test run names in S10_ROWS_FILE.</summary>
    public static void Line(string line)
    {
        var path = Environment.GetEnvironmentVariable("S10_ROWS_FILE");
        if (!string.IsNullOrEmpty(path))
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}

public class Props
{
    [Property(Seed = 42)]
    public void TheIntHolds(int x)
    {
    }

    [Property(Seed = 42)]
    public bool TheIntIsBelow10(int x) => x < 10;

    [Property]
    [Replay("0")]
    public bool ReplayZero(int x) => true;

    public static IGen<int> FiveGen() => Gen.Int32().Between(5, 5);

    [Property]
    public void MemberGenFive([MemberGen("FiveGen")] int x) => Rec.Line("five:" + x);

    [Sample]
    public IGen<int> SampleFive() => Gen.Int32().Between(5, 5);

    [Property]
    [InlineData(1)]
    [InlineData(2)]
    public void RowsData(int first, int second) => Rec.Line("rows:" + first + "," + second);
}

public class Forms
{
    [Property] public void FormVoidHolds(int x) { }
    [Property] public void FormVoidFalsified(int x) { if (x >= 10) throw new InvalidOperationException("too big"); }

    [Property] public bool FormBoolHolds(int x) => true;
    [Property] public bool FormBoolFalsified(int x) => x < 10;

    [Property] public Property FormPropertyHolds() => Property.ForAll(Gen.Int32(), x => true);
    [Property] public Property FormPropertyFalsified() => Property.ForAll(Gen.Int32(), x => x < 10);

    [Property] public IGen<Test> FormGenHolds() => Property.ForAll(Gen.Int32(), x => true);
    [Property] public IGen<Test> FormGenFalsified() => Property.ForAll(Gen.Int32(), x => x < 10);

    [Property] public async Task FormTaskHolds(int x) { await Task.Yield(); }
    [Property] public async Task FormTaskFalsified(int x) { await Task.Yield(); if (x >= 10) throw new InvalidOperationException("too big"); }

    [Property] public async Task<bool> FormTaskBoolHolds(int x) { await Task.Yield(); return true; }
    [Property] public async Task<bool> FormTaskBoolFalsified(int x) { await Task.Yield(); return x < 10; }

    [Property] public AsyncProperty FormAsyncPropertyHolds() => Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return true; });
    [Property] public AsyncProperty FormAsyncPropertyFalsified() => Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return x < 10; });

    [Property] public IGen<AsyncTest> FormAsyncGenHolds() => Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return true; });
    [Property] public IGen<AsyncTest> FormAsyncGenFalsified() => Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return x < 10; });
}

public class PublicTypes
{
    private static readonly string[] PropertyOptions = { "Iterations", "ShrinkLimit", "Seed", "Size", "TimeoutMilliseconds", "Exhaustive" };

    [Fact]
    public void TypesExist()
    {
        var assembly = typeof(PropertyAttribute).Assembly;
        Assert.Equal("GalaxyCheck.Xunit.V3", assembly.GetName().Name);

        var problems = new List<string>();
        Type? Attr(string name)
        {
            var type = assembly.GetType("GalaxyCheck." + name + "Attribute");
            if (type == null || !typeof(Attribute).IsAssignableFrom(type) || !type.IsPublic)
            {
                problems.Add("missing public attribute " + name + "Attribute");
                return null;
            }

            return type;
        }

        void Options(Type? type, params string[] names)
        {
            if (type == null) return;
            foreach (var name in names)
            {
                var p = type.GetProperty(name);
                if (p == null || !p.CanRead || !p.CanWrite) problems.Add(type.Name + " has no settable option " + name);
            }
        }

        void Ctor(Type? type)
        {
            if (type == null) return;
            if (type.GetConstructor(new[] { typeof(string) }) == null) problems.Add(type.Name + " has no constructor with a string");
        }

        Options(Attr("Property"), PropertyOptions);
        Attr("Sample");
        Options(Attr("GenSnapshot"), "Iterations", "Size");
        Ctor(Attr("Replay"));
        Ctor(Attr("MemberGen"));

        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }
}
