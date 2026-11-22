using GalaxyCheck;
using GalaxyCheck.Gens;

namespace S11.Forms;

public class FormTests
{
    // R23
    [Test, GalaxyCheck.PropertyTest(Seed = 1, Iterations = 100)]
    public bool IntBelow10(int x) => x < 10;

    [Test, GalaxyCheck.PropertyTest]
    public bool HoldingBool(int x) => true;

    // R24: each form, holding and falsified.
    [Test, GalaxyCheck.PropertyTest] public void Void_Holds(int x) { }
    [Test, GalaxyCheck.PropertyTest] public void Void_Falsified(int x) => throw new InvalidOperationException("falsified");

    [Test, GalaxyCheck.PropertyTest] public bool Bool_Holds(int x) => true;
    [Test, GalaxyCheck.PropertyTest] public bool Bool_Falsified(int x) => false;

    [Test, GalaxyCheck.PropertyTest] public Property<int> Property_Holds() => Property.ForAll(Gen.Int32(), x => true);
    [Test, GalaxyCheck.PropertyTest] public Property<int> Property_Falsified() => Property.ForAll(Gen.Int32(), x => false);

    [Test, GalaxyCheck.PropertyTest] public IGen<GalaxyCheck.Test> Gen_Holds() => Property.ForAll(Gen.Int32(), x => true);
    [Test, GalaxyCheck.PropertyTest] public IGen<GalaxyCheck.Test> Gen_Falsified() => Property.ForAll(Gen.Int32(), x => false);

    [Test, GalaxyCheck.PropertyTest] public async Task AsyncVoid_Holds(int x) { await Task.Yield(); }
    [Test, GalaxyCheck.PropertyTest] public async Task AsyncVoid_Falsified(int x) { await Task.Yield(); throw new InvalidOperationException("falsified"); }

    [Test, GalaxyCheck.PropertyTest] public async Task<bool> AsyncBool_Holds(int x) { await Task.Yield(); return true; }
    [Test, GalaxyCheck.PropertyTest] public async Task<bool> AsyncBool_Falsified(int x) { await Task.Yield(); return false; }

    [Test, GalaxyCheck.PropertyTest]
    public AsyncProperty<int> AsyncProperty_Holds() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return true; });

    [Test, GalaxyCheck.PropertyTest]
    public AsyncProperty<int> AsyncProperty_Falsified() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return false; });

    [Test, GalaxyCheck.PropertyTest]
    public IGen<GalaxyCheck.AsyncTest> AsyncGen_Holds() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return true; });

    [Test, GalaxyCheck.PropertyTest]
    public IGen<GalaxyCheck.AsyncTest> AsyncGen_Falsified() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return false; });

    // R25: the generator of the parameter comes from the named member.
    public static IGen<int> Five => Gen.Int32().Between(5, 5);

    [Test, GalaxyCheck.PropertyTest(Iterations = 5)]
    public void MemberGenFive([GalaxyCheck.MemberGen(nameof(Five))] int x) => Log.Write("MemberGenFive", x.ToString());

    // R26
    [Test, GalaxyCheck.Sample]
    public IGen<int> SampleConstant() => Gen.Int32().Between(7777, 7777);

    // R28
    [Test, GalaxyCheck.PropertyTest(Iterations = 5)]
    [GalaxyCheck.PropertyArguments(1)]
    [GalaxyCheck.PropertyArguments(2)]
    public void ArgumentRows(int first, int second) => Log.Write("ArgumentRows", $"{first},{second}");
}
