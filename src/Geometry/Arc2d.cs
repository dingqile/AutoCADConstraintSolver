namespace AutoCADConstraintSolver.Geometry;

/// <summary>
/// 2D arc defined by center, radius, start angle, and end angle
/// </summary>
public class Arc2d
{
    public Vec2 Center { get; set; }
    public double Radius { get; set; }
    public double StartAngle { get; set; }
    public double EndAngle { get; set; }
    
    /// <summary>
    /// Whether the arc goes counterclockwise (otherwise clockwise)
    /// </summary>
    public bool IsCounterClockwise { get; set; } = true;

    public Arc2d()
    {
    }

    public Arc2d(Vec2 center, double radius, double startAngle, double endAngle, bool counterClockwise = true)
    {
        Center = center;
        Radius = radius;
        StartAngle = startAngle;
        EndAngle = endAngle;
        IsCounterClockwise = counterClockwise;
    }

    /// <summary>
    /// Get the arc length
    /// </summary>
    public double Length
    {
        get
        {
            var sweepAngle = SweepAngle;
            return Radius * Math.Abs(sweepAngle);
        }
    }

    /// <summary>
    /// Get the sweep angle of the arc
    /// </summary>
    public double SweepAngle
    {
        get
        {
            var angle = EndAngle - StartAngle;
            if (IsCounterClockwise)
            {
                while (angle < 0) angle += 2 * Math.PI;
                while (angle > 2 * Math.PI) angle -= 2 * Math.PI;
            }
            else
            {
                while (angle > 0) angle -= 2 * Math.PI;
                while (angle < -2 * Math.PI) angle += 2 * Math.PI;
            }
            return angle;
        }
    }

    /// <summary>
    /// Get the start point of the arc
    /// </summary>
    public Vec2 StartPoint => Center + new Vec2(
        Radius * Math.Cos(StartAngle),
        Radius * Math.Sin(StartAngle));

    /// <summary>
    /// Get the end point of the arc
    /// </summary>
    public Vec2 EndPoint => Center + new Vec2(
        Radius * Math.Cos(EndAngle),
        Radius * Math.Sin(EndAngle));

    /// <summary>
    /// Get a point on the arc at parameter t (0 to 1)
    /// </summary>
    public Vec2 GetPointAt(double t)
    {
        var angle = StartAngle + t * SweepAngle;
        return Center + new Vec2(
            Radius * Math.Cos(angle),
            Radius * Math.Sin(angle));
    }

    /// <summary>
    /// Find the closest point on this arc to a given point
    /// </summary>
    public Vec2 ClosestPoint(Vec2 point)
    {
        var toPoint = point - Center;
        var angle = Math.Atan2(toPoint.Y, toPoint.X);
        
        if (!IsPointOnArc(angle))
        {
            var startDiff = Math.Abs(NormalizeAngle(angle - StartAngle));
            var endDiff = Math.Abs(NormalizeAngle(angle - EndAngle));
            
            return startDiff < endDiff ? StartPoint : EndPoint;
        }

        var direction = new Vec2(Math.Cos(angle), Math.Sin(angle));
        return Center + direction * Radius;
    }

    private bool IsPointOnArc(double angle)
    {
        angle = NormalizeAngle(angle);
        var start = NormalizeAngle(StartAngle);
        var end = NormalizeAngle(EndAngle);

        if (IsCounterClockwise)
        {
            return angle >= start && angle <= end;
        }
        else
        {
            return angle <= start && angle >= end;
        }
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle < 0) angle += 2 * Math.PI;
        while (angle > 2 * Math.PI) angle -= 2 * Math.PI;
        return angle;
    }

    /// <summary>
    /// Get the tangent direction at a point on the arc (parameter t from 0 to 1)
    /// </summary>
    public Vec2 GetTangentAt(double t)
    {
        var angle = StartAngle + t * SweepAngle;
        var tangent = new Vec2(-Math.Sin(angle), Math.Cos(angle));
        return IsCounterClockwise ? tangent : -tangent;
    }

    /// <summary>
    /// Get the tangent vector at the start point
    /// </summary>
    public Vec2 StartTangent => GetTangentAt(0);

    /// <summary>
    /// Get the tangent vector at the end point
    /// </summary>
    public Vec2 EndTangent => GetTangentAt(1);

    public override string ToString() => 
        $"Arc2d(Center={Center}, Radius={Radius:F4}, StartAngle={StartAngle:F4}, EndAngle={EndAngle:F4})";
}
