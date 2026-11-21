using GalaxyCheck;
namespace GalaxyCheck.Tests.Slice;
public class S12_Scratch {
    public void C() { IGen<Property.Test> a = Property.ForAll(Gen.Int32(), x => x < 10);
      IGen<Property.Test> b = Property.ForAll(Gen.Int32(), x => x < 10).Advanced;
      Property.Test c = Property.ForAll(Gen.Int32(), x => x < 10).Advanced; 
      IGen<Property.AsyncTest> d = Property.ForAllAsync(Gen.Int32(), async x => true); }
}
