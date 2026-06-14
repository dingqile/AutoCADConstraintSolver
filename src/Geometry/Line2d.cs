namespace AutoCADConstraintSolver.Geometry;

/// <summary>
/// 2D line defined by two points
/// </summary>
public class Line2d
{
    public Vec2 Start { get; set; }
    public Vec2 End { get; set; }

    public Line2d(Vec2 start, Vec2 end)
    {
        Start = start;
        End = end;
    }

    public Vec2 Direction => (End - Start).Normalized;
    
    public Vec2 Normal => Direction.Perp();

    public double Length => Vec2.Distance(Start, End);
    
    public double LengthSquared => Vec2.DistanceSquared(Start, End);

    public Vec2 GetPointAt(double t) => Vec2.Lerp(Start, End, t);

    /// <summary>
    /// Find the closest point on this line to a given point
    /// </summary>
    public Vec2 ClosestPoint(Vec2 point)
    {
        var v = End - Start;
        var w = point - Start;
        var c1 = Vec2.Dot(w, v);
        if (c1 <= 0) return Start;
        var c2 = Vec2.Dot(v, v);
        if (c2 <= c1) return End;
        var t = c1 / c2;
        return Start + t * v;
    }

    /// <summary>
    /// Calculate distance from a point to this line
    /// </summary>
    public double DistanceTo(Vec2 point)
    {
        var closest = ClosestPoint(point);
        return Vec2.Distance(point, closest);
    }

    /// <summary>
    /// Find intersection with another line
    /// </summary>
    public bool TryIntersect(Line2d other, out Vec2 intersection)
    {
        intersection = Vec2.Zero;
        
        var p1 = Start;
        var p2 = End;
        var p3 = other.Start;
        var p4 = other.End;

        var d1 = p2 - p1;
        var d2 = p4 - p3;
        var d3 = p1 - p3;

        var cross = Vec2.Cross(d1, d2);
        
        if (Math.Abs(cross) < 1e-10)
            return false;

        var t = Vec2.Cross(d2, d3) / cross;
        var u = Vec2.Cross(d1, d3) / cross;

        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            intersection = p1 + t * d1;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Find intersection with a circle
    /// </summary>
    public bool TryIntersect(Circle2d circle, out Vec2 intersection1, out Vec2 intersection2)
    {
        return circle.TryIntersect(this, out intersection1, out intersection2);
    }

    public override string ToString() => $"Line2d({Start} -> {End})";
}
