namespace AutoCADConstraintSolver.Geometry;

/// <summary>
/// 2D vector class for geometric calculations
/// </summary>
public struct Vec2
{
    public double X { get; set; }
    public double Y { get; set; }

    public Vec2(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double Magnitude => Math.Sqrt(X * X + Y * Y);
    
    public double MagnitudeSquared => X * X + Y * Y;

    public Vec2 Normalized
    {
        get
        {
            var mag = Magnitude;
            return mag > 1e-10 ? new Vec2(X / mag, Y / mag) : Vec2.Zero;
        }
    }

    public static Vec2 Zero => new(0, 0);
    public static Vec2 UnitX => new(1, 0);
    public static Vec2 UnitY => new(0, 1);

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, double scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vec2 operator *(double scalar, Vec2 a) => new(a.X * scalar, a.Y * scalar);
    public static Vec2 operator /(Vec2 a, double scalar) => new(a.X / scalar, a.Y / scalar);
    public static Vec2 operator -(Vec2 a) => new(-a.X, -a.Y);

    public static double Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
    
    public static double Cross(Vec2 a, Vec2 b) => a.X * b.Y - a.Y * b.X;

    public static Vec2 Lerp(Vec2 a, Vec2 b, double t) => a + (b - a) * t;

    public static double Distance(Vec2 a, Vec2 b) => (a - b).Magnitude;
    
    public static double DistanceSquared(Vec2 a, Vec2 b) => (a - b).MagnitudeSquared;

    public Vec2 Rotate(double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new Vec2(X * cos - Y * sin, X * sin + Y * cos);
    }

    public Vec2 Perp() => new(-Y, X);

    public override string ToString() => $"({X:F4}, {Y:F4})";

    public override bool Equals(object? obj) => obj is Vec2 other && Equals(other);
    
    public bool Equals(Vec2 other, double tolerance = 1e-6) =>
        Math.Abs(X - other.X) < tolerance && Math.Abs(Y - other.Y) < tolerance;

    public override int GetHashCode() => HashCode.Combine(X, Y);
}
