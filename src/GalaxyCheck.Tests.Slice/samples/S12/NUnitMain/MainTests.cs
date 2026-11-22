using GalaxyCheck;

namespace Sample;

[NUnit.Framework.TestFixture]
public class MainTests
{
    // R23: a property that holds, and a property that is falsified.
    [Property]
    public void IntHolds(int x)
    {
    }

    [Property(Seed = 1234)]
    public bool IntIsBelow10(int x) => x < 10;

    // R24: each method form, holding and falsified.
    [Property]
    public void Void_Holds(int x)
    {
    }

    [Property]
    public void Void_Falsified(int x)
    {
        if (x >= 10) throw new InvalidOperationException("too big");
    }

    [Property]
    public bool Bool_Holds(int x) => true;

    [Property]
    public bool Bool_Falsified(int x) => x < 10;

    [Property]
    public Property Property_Holds() => Property.ForAll(Gen.Int32(), x => true);

    [Property]
    public Property Property_Falsified() => Property.ForAll(Gen.Int32(), x => x < 10);

    [Property]
    public IGen<Property.Test> Gen_Holds() =>
        Property.ForAll(Gen.Int32(), x => true).Select(t => (Property.Test)t);

    [Property]
    public IGen<Property.Test> Gen_Falsified() =>
        Property.ForAll(Gen.Int32(), x => x < 10).Select(t => (Property.Test)t);

    [Property]
    public async Task AsyncVoid_Holds(int x)
    {
        await Task.Yield();
    }

    [Property]
    public async Task AsyncVoid_Falsified(int x)
    {
        await Task.Yield();
        if (x >= 10) throw new InvalidOperationException("too big");
    }

    [Property]
    public async Task<bool> AsyncBool_Holds(int x)
    {
        await Task.Yield();
        return true;
    }

    [Property]
    public async Task<bool> AsyncBool_Falsified(int x)
    {
        await Task.Yield();
        return x < 10;
    }

    [Property]
    public AsyncProperty AsyncProperty_Holds() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return true; });

    [Property]
    public AsyncProperty AsyncProperty_Falsified() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return x < 10; });

    [Property]
    public IGen<Property.AsyncTest> AsyncGen_Holds() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return true; })
            .Select(t => (Property.AsyncTest)t);

    [Property]
    public IGen<Property.AsyncTest> AsyncGen_Falsified() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return x < 10; })
            .Select(t => (Property.AsyncTest)t);

    // R25: the parameter takes its generator from a member.
    public static IGen<int> FiveGen => Gen.Int32().Between(5, 5);

    [Property(Iterations = 5)]
    public void MemberGenIsFive([MemberGen(nameof(FiveGen))] int x)
    {
        Probe.Append("member", x.ToString());
        if (x != 5) throw new InvalidOperationException("The parameter is " + x);
    }

    // R26: a sample test fails and lists the sampled values.
    [Sample]
    public IGen<int> Sampled() => Gen.Int32().Between(7777, 7777);

    // R28: data rows give the first parameter, the generator gives the second.
    [Property(Iterations = 5)]
    [NUnit.Framework.TestCase(1)]
    [NUnit.Framework.TestCase(2)]
    public void Rows(int a, int b)
    {
        Probe.Append("rows", a + "," + b);
        if (a != 1 && a != 2) throw new InvalidOperationException("The first parameter is " + a);
    }

    // R22: the public types of the package.
    [NUnit.Framework.Test]
    public void PublicTypes()
    {
        var assembly = typeof(PropertyAttribute).Assembly;
        var missing = new List<string>();

        void RequireType(string name, params string[] properties)
        {
            var type = assembly.GetType("GalaxyCheck." + name);
            if (type is null || !type.IsPublic)
            {
                missing.Add("type " + name);
                return;
            }

            foreach (var property in properties)
            {
                if (type.GetProperty(property) is null) missing.Add(name + "." + property);
            }
        }

        RequireType("PropertyAttribute", "Iterations", "ShrinkLimit", "Seed", "Size", "TimeoutMilliseconds", "Exhaustive");
        RequireType("SampleAttribute");
        RequireType("GenSnapshotAttribute", "Iterations", "Size");
        RequireType("ReplayAttribute");
        RequireType("MemberGenAttribute");

        foreach (var name in new[] { "ReplayAttribute", "MemberGenAttribute" })
        {
            var constructor = assembly.GetType("GalaxyCheck." + name)?
                .GetConstructor(new[] { typeof(string) });
            if (constructor is null) missing.Add(name + "(string)");
        }

        if (missing.Count > 0) throw new InvalidOperationException("Missing: " + string.Join(", ", missing));
    }
}
