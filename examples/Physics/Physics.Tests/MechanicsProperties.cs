using GalaxyCheck;

namespace Physics.Tests;

public class KinematicsProperties
{
    public static IGen<double> Distance => PhysicsGen.Between(0, 1_000);

    // A speed is 0 or from 0.001 to 100. The first runs of this property found that a subnormal speed or
    // acceleration, such as 5e-324, underflows to 0 when it is squared or halved. The bounds keep these inputs
    // out, and the vector properties test that range on purpose.
    public static IGen<double> Speed => Gen.Choose((Gen.Constant(0.0), 1), (PhysicsGen.Between(0.001, 100), 4));

    // Zero gets its own share: a range of doubles almost never hits exactly 0 by chance.
    public static IGen<double> Acceleration => Gen.Choose(
        (Gen.Constant(0.0), 1),
        (PhysicsGen.Between(0.001, 10), 2),
        (PhysicsGen.Between(-10, -0.001), 2));

    public static IGen<(double Distance, double Speed, double Acceleration)> Trips =>
        from distance in Distance
        from speed in Speed
        from acceleration in Acceleration
        select (distance, speed, acceleration);

    // The body slows down, and its stopping distance v0^2 / 2|a| is less than the distance.
    private static bool StopsShort((double Distance, double Speed, double Acceleration) trip) =>
        trip.Acceleration < 0 && trip.Speed * trip.Speed / (-2 * trip.Acceleration) < trip.Distance;

    // Inverse: the time to reach a distance, put back into the equation of motion, gives the distance. If no time
    // comes back, the body must stop short or never move.
    [Test, PropertyTest(Iterations = 300)]
    public Property<(double Distance, double Speed, double Acceleration)> TimeToReachIsTheInverseOfPosition() =>
        Property.ForAll(Trips, trip =>
            {
                var (d, v0, a) = trip;
                var t = Kinematics.TimeToReach(d, v0, a);
                if (t is null)
                {
                    return d > 0 && (StopsShort(trip) || (v0 == 0 && a == 0));
                }

                var arrivesThere = Math.Abs(Kinematics.Position(0, v0, a, t.Value) - d) <= 1e-9 * Math.Max(1, d);
                var stillMovingForward = Kinematics.Velocity(v0, a, t.Value) >= -1e-9 * Math.Max(1, v0);
                return t >= 0 && arrivesThere && stillMovingForward;
            })
            .Cover("constant speed (a = 0)", 10, trip => trip.Acceleration == 0)
            .Cover("stops short", 5, StopsShort)
            .Label("speeds up", trip => trip.Acceleration > 0)
            .Label("slows down", trip => trip.Acceleration < 0);
}

public class CollisionProperties
{
    public static IGen<(Body A, Body B)> Pairs => PhysicsGen.TouchingPair;

    private static bool Close(Vector3 expected, Vector3 actual, double scale) =>
        (expected - actual).Length <= 1e-10 * Math.Max(scale, 1e-300);

    [Test, PropertyTest]
    public bool MomentumIsConserved([MemberGen(nameof(Pairs))] (Body A, Body B) pair)
    {
        var (a, b) = Collision.Elastic(pair.A, pair.B);
        var scale = pair.A.Momentum.Length + pair.B.Momentum.Length;
        return Close(pair.A.Momentum + pair.B.Momentum, a.Momentum + b.Momentum, scale);
    }

    [Test, PropertyTest]
    public bool KineticEnergyIsConserved([MemberGen(nameof(Pairs))] (Body A, Body B) pair)
    {
        var (a, b) = Collision.Elastic(pair.A, pair.B);
        var before = pair.A.KineticEnergy + pair.B.KineticEnergy;
        return Math.Abs(before - (a.KineticEnergy + b.KineticEnergy)) <= 1e-10 * before;
    }

    // Symmetry: the order of the two bodies does not matter. The arithmetic is mirrored, so the match is exact.
    [Test, PropertyTest]
    public bool SwappingTheBodiesSwapsTheResult([MemberGen(nameof(Pairs))] (Body A, Body B) pair)
    {
        var (a, b) = Collision.Elastic(pair.A, pair.B);
        var (b2, a2) = Collision.Elastic(pair.B, pair.A);
        return a == a2 && b == b2;
    }

    public static IGen<Vector3> FrameVelocity => PhysicsGen.Vector(100);

    // Metamorphic: an observer who moves at a constant velocity sees the same collision, shifted by that velocity.
    [Test, PropertyTest]
    public bool CollisionLooksTheSameFromAMovingFrame(
        [MemberGen(nameof(Pairs))] (Body A, Body B) pair,
        [MemberGen(nameof(FrameVelocity))] Vector3 frame)
    {
        var (a, b) = Collision.Elastic(pair.A, pair.B);
        var (movedA, movedB) = Collision.Elastic(
            pair.A with { Velocity = pair.A.Velocity + frame },
            pair.B with { Velocity = pair.B.Velocity + frame });

        var scale = pair.A.Velocity.Length + pair.B.Velocity.Length + frame.Length;
        return Close(a.Velocity + frame, movedA.Velocity, scale) && Close(b.Velocity + frame, movedB.Velocity, scale);
    }

    [Test, PropertyTest]
    public bool BodiesThatMoveApartKeepTheirVelocities([MemberGen(nameof(Pairs))] (Body A, Body B) pair)
    {
        var apart = (pair.A.Velocity - pair.B.Velocity).Dot(pair.A.Position - pair.B.Position) >= 0;
        Property.Precondition(apart);
        return Collision.Elastic(pair.A, pair.B) == pair;
    }
}

public class IntegratorProperties
{
    public static IGen<Spring> Springs =>
        from stiffness in PhysicsGen.Between(0.1, 1_000)
        from mass in PhysicsGen.Between(0.1, 100)
        select new Spring(stiffness, mass);

    // The step is a fraction of one period: from 1/6000 to 1/60 of 2 pi / omega.
    public static IGen<double> StepFraction => PhysicsGen.Between(0.001, 0.1);

    public static IGen<double> Start => PhysicsGen.Between(-10, 10);

    [Test, PropertyTest(Iterations = 50)]
    public bool SpringEnergyStaysWithinOnePercentOverManySteps(
        [MemberGen(nameof(Springs))] Spring spring,
        [MemberGen(nameof(StepFraction))] double stepFraction,
        [MemberGen(nameof(Start))] double x0,
        [MemberGen(nameof(Start))] double v0)
    {
        Property.Precondition(x0 != 0 || v0 != 0);
        var dt = stepFraction / spring.AngularFrequency;
        var (x, v) = (x0, v0);
        for (var i = 0; i < 10_000; i++)
        {
            (x, v) = spring.Step(x, v, dt);
        }

        var start = spring.Energy(x0, v0);
        return Math.Abs(spring.Energy(x, v) - start) <= 0.01 * start;
    }

    // Oracle: the numerical method agrees with the exact solution after one full period.
    [Test, PropertyTest(Iterations = 50)]
    public bool SpringFollowsTheExactSolution(
        [MemberGen(nameof(Springs))] Spring spring,
        [MemberGen(nameof(Start))] double x0,
        [MemberGen(nameof(Start))] double v0)
    {
        const int steps = 1_000;
        var period = 2 * Math.PI / spring.AngularFrequency;
        var dt = period / steps;
        var (x, v) = (x0, v0);
        for (var i = 0; i < steps; i++)
        {
            (x, v) = spring.Step(x, v, dt);
        }

        var amplitude = Math.Sqrt(x0 * x0 + v0 / spring.AngularFrequency * (v0 / spring.AngularFrequency));
        return Math.Abs(x - spring.ExactPosition(x0, v0, period)) <= 1e-3 * Math.Max(amplitude, 1e-12);
    }

    /// <summary>Bound orbits: a radius from 1 to 10, and a speed from 0.8 to 1.2 times the speed of a circular orbit.</summary>
    public static IGen<(Vector3 Position, Vector3 Velocity)> Orbits =>
        from radius in PhysicsGen.Between(1, 10)
        from direction in PhysicsGen.Direction
        from other in PhysicsGen.Direction
        from speedFactor in PhysicsGen.Between(0.8, 1.2)
        let sideways = direction.Cross(other)
        where sideways.Length > 0.1
        select (radius * direction, speedFactor * Math.Sqrt(1 / radius) * sideways.Normalized());

    [Test, PropertyTest(Iterations = 30)]
    public bool OrbitKeepsItsAngularMomentum([MemberGen(nameof(Orbits))] (Vector3 Position, Vector3 Velocity) start)
    {
        var orbit = new Orbit(Mu: 1);
        var (r, v) = start;
        for (var i = 0; i < 5_000; i++)
        {
            (r, v) = orbit.Step(r, v, dt: 0.01);
        }

        var before = Orbit.AngularMomentum(start.Position, start.Velocity);
        return (Orbit.AngularMomentum(r, v) - before).Length <= 1e-9 * before.Length;
    }

    [Test, PropertyTest(Iterations = 30)]
    public bool OrbitEnergyStaysNearItsStartValue([MemberGen(nameof(Orbits))] (Vector3 Position, Vector3 Velocity) start)
    {
        var orbit = new Orbit(Mu: 1);
        var (r, v) = start;
        for (var i = 0; i < 5_000; i++)
        {
            (r, v) = orbit.Step(r, v, dt: 0.01);
        }

        var before = orbit.SpecificEnergy(start.Position, start.Velocity);
        return Math.Abs(orbit.SpecificEnergy(r, v) - before) <= 1e-3 * Math.Abs(before);
    }
}
