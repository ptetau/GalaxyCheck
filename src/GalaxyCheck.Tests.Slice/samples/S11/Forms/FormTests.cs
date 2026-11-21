using GalaxyCheck;
using GalaxyCheck.Gens;

namespace S11.Forms;

public class FormTests
{
    // R23
    [GalaxyCheck.Property(Seed = 1, Iterations = 100)]
    public bool IntBelow10(int x) => x < 10;

    [GalaxyCheck.Property]
    public bool HoldingBool(int x) => true;

    // R24: each form, holding and falsified.
    [GalaxyCheck.Property] public void Void_Holds(int x) { }
    [GalaxyCheck.Property] public void Void_Falsified(int x) => throw new InvalidOperationException("falsified");

    [GalaxyCheck.Property] public bool Bool_Holds(int x) => true;
    [GalaxyCheck.Property] public bool Bool_Falsified(int x) => false;

    [GalaxyCheck.Property] public Property<int> Property_Holds() => Property.ForAll(Gen.Int32(), x => true);
    [GalaxyCheck.Property] public Property<int> Property_Falsified() => Property.ForAll(Gen.Int32(), x => false);

    [GalaxyCheck.Property] public IGen<GalaxyCheck.Test> Gen_Holds() => Property.ForAll(Gen.Int32(), x => true);
    [GalaxyCheck.Property] public IGen<GalaxyCheck.Test> Gen_Falsified() => Property.ForAll(Gen.Int32(), x => false);

    [GalaxyCheck.Property] public async Task AsyncVoid_Holds(int x) { await Task.Yield(); }
    [GalaxyCheck.Property] public async Task AsyncVoid_Falsified(int x) { await Task.Yield(); throw new InvalidOperationException("falsified"); }

    [GalaxyCheck.Property] public async Task<bool> AsyncBool_Holds(int x) { await Task.Yield(); return true; }
    [GalaxyCheck.Property] public async Task<bool> AsyncBool_Falsified(int x) { await Task.Yield(); return false; }

    [GalaxyCheck.Property]
    public AsyncProperty<int> AsyncProperty_Holds() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return true; });

    [GalaxyCheck.Property]
    public AsyncProperty<int> AsyncProperty_Falsified() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return false; });

    [GalaxyCheck.Property]
    public IGen<GalaxyCheck.AsyncTest> AsyncGen_Holds() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return true; });

    [GalaxyCheck.Property]
    public IGen<GalaxyCheck.AsyncTest> AsyncGen_Falsified() =>
        Property.ForAllAsync(Gen.Int32(), async x => { await Task.Yield(); return false; });

    // R25: the generator of the parameter comes from the named member.
    public static IGen<int> Five => Gen.Int32().Between(5, 5);

    [GalaxyCheck.Property(Iterations = 5)]
    public void MemberGenFive([GalaxyCheck.MemberGen(nameof(Five))] int x) => Log.Write("MemberGenFive", x.ToString());

    // R26
    [GalaxyCheck.Sample]
    public IGen<int> SampleConstant() => Gen.Int32().Between(7777, 7777);

    // R28
    [GalaxyCheck.Property(Iterations = 5)]
    [TUnit.Core.Arguments(1)]
    [TUnit.Core.Arguments(2)]
    public void ArgumentRows(int first, int second) => Log.Write("ArgumentRows", $"{first},{second}");
}
