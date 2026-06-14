namespace AutoCADConstraintSolver.Geometry;

/// <summary>
/// 2D circle defined by center and radius
/// </summary>
public class Circle2d
{
    public Vec2 Center { get; set; }
    public double Radius { get; set; }

    public Circle2d(Vec2 center, double radius)
    {
        Center = center;
        Radius = radius;
    }

    public double Diameter => Radius * 2;
    
    public double Circumference => 2 * Math.PI * Radius;
    
    public double Area => Math.PI * Radius * Radius;

    /// <summary>
    /// Get the tangent point closest to a given point
    /// </summary>
    public Vec2 ClosestPoint(Vec2 point)
    {
        var direction = (point - Center).Normalized;
        return Center + direction * Radius;
    }

    /// <summary>
    /// Calculate distance from a point to the circle
    /// </summary>
    public double DistanceTo(Vec2 point)
    {
        return Math.Abs(Vec2.Distance(point, Center) - Radius);
    }

    /// <summary>
    /// Check if a point is inside the circle
    /// </summary>
    public bool ContainsPoint(Vec2 point)
    {
        return Vec2.DistanceSquared(point, Center) <= Radius * Radius;
    }

    /// <summary>
    /// Find intersection points with a line
    /// </summary>
    public bool TryIntersect(Line2d line, out Vec2 intersection1, out Vec2 intersection2)
    {
        intersection1 = Vec2.Zero;
        intersection2 = Vec2.Zero;

        var dx = line.End.X - line.Start.X;
        var dy = line.End.Y - line.Start.Y;

        var fx = line.Start.X - Center.X;
        var fy = line.Start.Y - Center.Y;

        var a = dx * dx + dy * dy;
        var b = 2 * (fx * dx + fy * dy);
        var c = fx * fx + fy * fy - Radius * Radius;

        var discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
            return false;

        var sqrtDiscriminant = Math.Sqrt(discriminant);
        var t1 = (-b - sqrtDiscriminant) / (2 * a);
        var t2 = (-b + sqrtDiscriminant) / (2 * a);

        intersection1 = line.Start + t1 * new Vec2(dx, dy);
        intersection2 = line.Start + t2 * new Vec2(dx, dy);

        return true;
    }

    /// <summary>
    /// Find intersection points with another circle
    /// </summary>
    public bool TryIntersect(Circle2d other, out Vec2 intersection1, out Vec2 intersection2)
    {
        intersection1 = Vec2.Zero;
        intersection2 = Vec2.Zero;

        var d = Vec2.Distance(Center, other.Center);
        
        if (d > Radius + other.Radius || d < Math.Abs(Radius - other.Radius))
            return false;

        var a = (Radius * Radius - other.Radius * other.Radius + d * d) / (2 * d);
        var h = Math.Sqrt(Radius * Radius - a * a);

        var px = Center.X + (a / d) * (other.Center.X - Center.X);
        var py = Center.Y + (a / d) * (other.Center.Y - Center.Y);

        var rx = -(other.Center.Y - Center.Y) * (h / d);
        var ry = (other.Center.X - Center.X) * (h / d);

        intersection1 = new Vec2(px + rx, py + ry);
        intersection2 = new Vec2(px - rx, py - ry);

        return true;
    }

    public override string ToString() => $"Circle2d(Center={Center}, Radius={Radius:F4})";
}
