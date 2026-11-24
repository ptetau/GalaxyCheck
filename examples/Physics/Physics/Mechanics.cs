namespace Physics;

/// <summary>Motion along a line with a constant acceleration.</summary>
public static class Kinematics
{
    public static double Position(double x0, double v0, double a, double t) => x0 + v0 * t + 0.5 * a * t * t;

    public static double Velocity(double v0, double a, double t) => v0 + a * t;

    /// <summary>
    /// The first time at which a body that starts at position 0 with speed <paramref name="v0"/> (0 or more) reaches
    /// <paramref name="distance"/> (0 or more). Returns null if the body stops before it gets there, or never moves.
    /// </summary>
    public static double? TimeToReach(double distance, double v0, double a)
    {
        if (distance < 0 || v0 < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), "The distance and the start speed must be 0 or more.");
        }

        if (distance == 0)
        {
            return 0;
        }

        // From rest, the time is sqrt(2d / a). The general formula below would square tiny values and underflow.
        if (v0 == 0)
        {
            return a > 0 ? Finite(Math.Sqrt(2 * distance / a)) : null;
        }

        var discriminant = v0 * v0 + 2 * a * distance;
        if (discriminant < 0)
        {
            return null;
        }

        // The form 2d / (v0 + sqrt(D)) of the quadratic root has no cancellation, and works for a = 0. A speed so
        // small that the time overflows to infinity counts as no movement.
        return Finite(2 * distance / (v0 + Math.Sqrt(discriminant)));
    }

    private static double? Finite(double time) => double.IsFinite(time) ? time : null;
}

/// <summary>A point mass.</summary>
public readonly record struct Body(Vector3 Position, Vector3 Velocity, double Mass)
{
    public Vector3 Momentum => Mass * Velocity;

    public double KineticEnergy => 0.5 * Mass * Velocity.Dot(Velocity);
}

public static class Collision
{
    /// <summary>
    /// The velocities after an elastic collision of two spheres that touch. The impulse acts along the line between
    /// the centres. Spheres that move apart do not collide, and keep their velocities.
    /// </summary>
    public static (Body A, Body B) Elastic(Body a, Body b)
    {
        if (a.Mass <= 0 || b.Mass <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(a), "Each mass must be above zero.");
        }

        var normal = (a.Position - b.Position).Normalized();
        var approach = (a.Velocity - b.Velocity).Dot(normal);
        if (approach >= 0)
        {
            return (a, b);
        }

        var impulse = 2 * approach / (a.Mass + b.Mass);
        return (
            a with { Velocity = a.Velocity - impulse * b.Mass * normal },
            b with { Velocity = b.Velocity + impulse * a.Mass * normal });
    }
}

/// <summary>A mass on a spring, with no friction: x'' = -(k/m) x.</summary>
public sealed record Spring(double Stiffness, double Mass)
{
    public double AngularFrequency => Math.Sqrt(Stiffness / Mass);

    public double Energy(double x, double v) => 0.5 * Stiffness * x * x + 0.5 * Mass * v * v;

    /// <summary>One step of the velocity Verlet method, which keeps the energy close to its start value over long runs.</summary>
    public (double X, double V) Step(double x, double v, double dt)
    {
        var a = -Stiffness / Mass * x;
        var halfV = v + 0.5 * dt * a;
        var nextX = x + dt * halfV;
        var nextA = -Stiffness / Mass * nextX;
        return (nextX, halfV + 0.5 * dt * nextA);
    }

    /// <summary>The exact position at time t, for the start position x0 and the start velocity v0.</summary>
    public double ExactPosition(double x0, double v0, double t)
    {
        var w = AngularFrequency;
        return x0 * Math.Cos(w * t) + v0 / w * Math.Sin(w * t);
    }
}

/// <summary>A small body around a fixed central mass, with the gravitational parameter mu = G M.</summary>
public sealed record Orbit(double Mu)
{
    public Vector3 Acceleration(Vector3 position)
    {
        var r = position.Length;
        return -Mu / (r * r * r) * position;
    }

    /// <summary>One leapfrog step (kick, drift, kick). The force is central, so each step keeps the angular momentum.</summary>
    public (Vector3 Position, Vector3 Velocity) Step(Vector3 position, Vector3 velocity, double dt)
    {
        var halfV = velocity + 0.5 * dt * Acceleration(position);
        var nextPosition = position + dt * halfV;
        return (nextPosition, halfV + 0.5 * dt * Acceleration(nextPosition));
    }

    public static Vector3 AngularMomentum(Vector3 position, Vector3 velocity) => position.Cross(velocity);

    public double SpecificEnergy(Vector3 position, Vector3 velocity) =>
        0.5 * velocity.Dot(velocity) - Mu / position.Length;
}
