namespace Physics;

/// <summary>A vector in three dimensions, in SI units.</summary>
public readonly record struct Vector3(double X, double Y, double Z)
{
    public static readonly Vector3 Zero = new(0, 0, 0);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3 operator -(Vector3 a) => new(-a.X, -a.Y, -a.Z);

    public static Vector3 operator *(double s, Vector3 a) => new(s * a.X, s * a.Y, s * a.Z);

    public static Vector3 operator *(Vector3 a, double s) => s * a;

    public static Vector3 operator /(Vector3 a, double s) => new(a.X / s, a.Y / s, a.Z / s);

    public double Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3 Cross(Vector3 other) =>
        new(Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X);

    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);

    /// <summary>
    /// The Euclidean length. The components are scaled by the largest one first, so that a vector of very large or
    /// very small components does not overflow to infinity or underflow to zero on the way.
    /// </summary>
    public double Length
    {
        get
        {
            if (double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z))
            {
                return double.NaN;
            }

            var scale = Math.Max(Math.Abs(X), Math.Max(Math.Abs(Y), Math.Abs(Z)));
            if (scale == 0 || double.IsInfinity(scale))
            {
                return scale;
            }

            double x = X / scale, y = Y / scale, z = Z / scale;
            return scale * Math.Sqrt(x * x + y * y + z * z);
        }
    }

    /// <summary>The vector with the same direction and a length of 1. A zero or a non-finite vector has no direction.</summary>
    public Vector3 Normalized()
    {
        var length = Length;
        if (length == 0 || !double.IsFinite(length))
        {
            throw new InvalidOperationException($"The vector {this} has no direction.");
        }

        // Divide by the largest component first, so that the division by the length cannot underflow.
        var scale = Math.Max(Math.Abs(X), Math.Max(Math.Abs(Y), Math.Abs(Z)));
        return this / scale / (length / scale);
    }

    public bool ApproximatelyEquals(Vector3 other, double tolerance) => (this - other).Length <= tolerance;

    public override string ToString() => $"({X:R}, {Y:R}, {Z:R})";
}
