using GalaxyCheck;

namespace Sample;

[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
public class MainTests
{
    // R23: a property that holds, and a property that is falsified.
    [Property]
    public void IntHolds(int x)
    {
    }

    [Property(Seed = 1234)]
    public void IntIsBelow10(int x)
    {
        if (!(x < 10)) throw new Exception("the int is not below 10");
    }

    // R24 (D21): MSTest has the void and the Task form only.
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

    // R25: the parameter takes its generator from a member.
    public static IGen<int> FiveGen => Gen.Int32().Between(5, 5);

    [Property(Iterations = 5)]
    public void MemberGenIsFive([MemberGen(nameof(FiveGen))] int x)
    {
        Probe.Append("member", x.ToString());
        if (x != 5) throw new InvalidOperationException("The parameter is " + x);
    }

    // R26: a sample test fails and lists the sampled values.
    public static IGen<int> SampleGen => Gen.Int32().Between(7777, 7777);

    [Sample]
    public void Sampled([MemberGen(nameof(SampleGen))] int x)
    {
    }

    // R28: data rows give the first parameter, the generator gives the second.
    [Property(Iterations = 5)]
    [Microsoft.VisualStudio.TestTools.UnitTesting.DataRow(1)]
    [Microsoft.VisualStudio.TestTools.UnitTesting.DataRow(2)]
    public void Rows(int a, int b)
    {
        Probe.Append("rows", a + "," + b);
        if (a != 1 && a != 2) throw new InvalidOperationException("The first parameter is " + a);
    }

    // R22: the public types of the package.
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
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
