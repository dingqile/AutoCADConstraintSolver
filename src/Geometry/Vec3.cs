namespace AutoCADConstraintSolver.Geometry;

/// <summary>
/// 3D vector class for geometric calculations
/// </summary>
public struct Vec3
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Vec3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);
    
    public double MagnitudeSquared => X * X + Y * Y + Z * Z;

    public Vec3 Normalized
    {
        get
        {
            var mag = Magnitude;
            return mag > 1e-10 ? new Vec3(X / mag, Y / mag, Z / mag) : Vec3.Zero;
        }
    }

    public static Vec3 Zero => new(0, 0, 0);
    public static Vec3 UnitX => new(1, 0, 0);
    public static Vec3 UnitY => new(0, 1, 0);
    public static Vec3 UnitZ => new(0, 0, 1);

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(Vec3 a, double scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    public static Vec3 operator *(double scalar, Vec3 a) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    public static Vec3 operator /(Vec3 a, double scalar) => new(a.X / scalar, a.Y / scalar, a.Z / scalar);
    public static Vec3 operator -(Vec3 a) => new(-a.X, -a.Y, -a.Z);

    public static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    
    public static Vec3 Cross(Vec3 a, Vec3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    public static double Distance(Vec3 a, Vec3 b) => (a - b).Magnitude;
    
    public static double DistanceSquared(Vec3 a, Vec3 b) => (a - b).MagnitudeSquared;

    public Vec2 ToVec2() => new(X, Y);

    public override string ToString() => $"({X:F4}, {Y:F4}, {Z:F4})";

    public override bool Equals(object? obj) => obj is Vec3 other && Equals(other);
    
    public bool Equals(Vec3 other, double tolerance = 1e-6) =>
        Math.Abs(X - other.X) < tolerance && 
        Math.Abs(Y - other.Y) < tolerance && 
        Math.Abs(Z - other.Z) < tolerance;

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
}
