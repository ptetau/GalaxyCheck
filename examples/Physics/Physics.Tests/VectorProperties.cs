using GalaxyCheck;

namespace Physics.Tests;

public class VectorProperties
{
    // Cover fails the run if fewer than the given percent of cases have a label. Thus the run proves that it tried
    // very small and very large vectors, and not only the easy middle.
    [Test, PropertyTest(Iterations = 300)]
    public Property<Vector3> NormalizedVectorHasLengthOne() =>
        Property.ForAll(PhysicsGen.AnyScaleVector, v =>
            {
                Property.Precondition(v != Vector3.Zero);
                return Math.Abs(v.Normalized().Length - 1) <= 1e-15 * 4;
            })
            .Cover("tiny (below 1e-150)", 10, v => v.Length < 1e-150)
            .Cover("huge (above 1e150)", 10, v => v.Length > 1e150)
            .Cover("everyday (1e-10 to 1e10)", 1, v => v.Length is > 1e-10 and < 1e10)
            .Label("has a zero component", v => v.X == 0 || v.Y == 0 || v.Z == 0);

    public static IGen<Vector3> AnyScale => PhysicsGen.AnyScaleVector;

    [Test, PropertyTest]
    public bool LengthLiesBetweenTheLargestComponentAndRootThreeTimesIt([MemberGen(nameof(AnyScale))] Vector3 v)
    {
        var largest = Math.Max(Math.Abs(v.X), Math.Max(Math.Abs(v.Y), Math.Abs(v.Z)));
        return v.Length >= largest && v.Length <= Math.Sqrt(3) * largest * (1 + 1e-15);
    }

    public static IGen<double> Special => PhysicsGen.SpecialValue;

    public static IGen<double> Ordinary => PhysicsGen.Between(-1e6, 1e6);

    [Test, PropertyTest]
    public void SpecialComponentsGiveTheExpectedLength(
        [MemberGen(nameof(Special))] double special,
        [MemberGen(nameof(Ordinary))] double other)
    {
        var length = new Vector3(other, special, other).Length;
        if (double.IsNaN(special))
        {
            Expect(double.IsNaN(length), "a NaN component gives a NaN length");
        }
        else if (double.IsInfinity(special))
        {
            Expect(double.IsPositiveInfinity(length), "an infinite component gives an infinite length");
        }
        else
        {
            Expect(double.IsFinite(length) && length >= Math.Abs(special), "a finite vector has a finite length");
        }
    }

    public static IGen<Vector3> Everyday => PhysicsGen.Vector(100);

    // Invariant: the cross product is at right angles to both of its inputs.
    [Test, PropertyTest]
    public bool CrossProductIsPerpendicularToBothInputs(
        [MemberGen(nameof(Everyday))] Vector3 a,
        [MemberGen(nameof(Everyday))] Vector3 b)
    {
        var cross = a.Cross(b);
        var tolerance = 1e-12 * a.Length * b.Length * Math.Max(a.Length, b.Length);
        return Math.Abs(cross.Dot(a)) <= tolerance && Math.Abs(cross.Dot(b)) <= tolerance;
    }

    [Test, PropertyTest]
    public bool CrossProductIsAntiSymmetric([MemberGen(nameof(Everyday))] Vector3 a, [MemberGen(nameof(Everyday))] Vector3 b) =>
        a.Cross(b) == -b.Cross(a);

    internal static void Expect(bool condition, string rule)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Expected: " + rule);
        }
    }
}
