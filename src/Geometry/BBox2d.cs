namespace AutoCADConstraintSolver.Geometry;

/// <summary>
/// 2D bounding box
/// </summary>
public class BBox2d
{
    public Vec2 Min { get; set; }
    public Vec2 Max { get; set; }

    public BBox2d()
    {
        Min = new Vec2(double.MaxValue, double.MaxValue);
        Max = new Vec2(double.MinValue, double.MinValue);
    }

    public BBox2d(Vec2 min, Vec2 max)
    {
        Min = min;
        Max = max;
    }

    public double Width => Max.X - Min.X;
    public double Height => Max.Y - Min.Y;
    public Vec2 Center => new((Min.X + Max.X) / 2, (Min.Y + Max.Y) / 2);
    
    public double Diagonal => Vec2.Distance(Min, Max);

    public bool IsEmpty => Min.X > Max.X || Min.Y > Max.Y;

    public void Expand(Vec2 point)
    {
        Min = new Vec2(Math.Min(Min.X, point.X), Math.Min(Min.Y, point.Y));
        Max = new Vec2(Math.Max(Max.X, point.X), Math.Max(Max.Y, point.Y));
    }

    public void Expand(BBox2d other)
    {
        if (!other.IsEmpty)
        {
            Expand(other.Min);
            Expand(other.Max);
        }
    }

    public void Expand(double margin)
    {
        Min = new Vec2(Min.X - margin, Min.Y - margin);
        Max = new Vec2(Max.X + margin, Max.Y + margin);
    }

    public bool Contains(Vec2 point)
    {
        return point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y;
    }

    public bool Intersects(BBox2d other)
    {
        return Min.X <= other.Max.X && Max.X >= other.Min.X &&
               Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;
    }

    public bool Contains(BBox2d other)
    {
        return Min.X <= other.Min.X && Max.X >= other.Max.X &&
               Min.Y <= other.Min.Y && Max.Y >= other.Max.Y;
    }

    public static BBox2d CreateFromPoints(params Vec2[] points)
    {
        var bbox = new BBox2d();
        foreach (var p in points)
            bbox.Expand(p);
        return bbox;
    }

    public override string ToString() => $"BBox2d({Min} -> {Max})";
}
