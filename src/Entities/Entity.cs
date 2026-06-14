using System;
using System.Collections.Generic;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Entities;

/// <summary>
/// Entity types supported by the constraint solver
/// </summary>
public enum EntityType
{
    Point,
    Line,
    Circle,
    Arc,
    Ellipse,
    EllipticArc,
    Spline
}

/// <summary>
/// Base class for all sketch entities
/// </summary>
public abstract class Entity
{
    /// <summary>Unique identifier for this entity</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Type of this entity</summary>
    public abstract EntityType Type { get; }

    /// <summary>Whether this entity is selected</summary>
    public bool IsSelected { get; set; }

    /// <summary>Whether this entity is construction geometry</summary>
    public bool IsConstruction { get; set; }

    /// <summary>
    /// Get the bounding box of this entity
    /// </summary>
    public abstract BBox2d GetBoundingBox();

    /// <summary>
    /// Get the distance from a point to this entity
    /// </summary>
    public abstract double DistanceTo(Vec2 point);

    /// <summary>
    /// Get the closest point on this entity to a given point
    /// </summary>
    public abstract Vec2 ClosestPoint(Vec2 point);

    /// <summary>
    /// Get parameters representing this entity for the constraint solver
    /// </summary>
    public abstract IEnumerable<Param> GetParams();

    /// <summary>
    /// Setup equations that define this entity
    /// </summary>
    public abstract void SetupEquations(EquationSystem system);

    /// <summary>
    /// Remove equations for this entity from the solver
    /// </summary>
    public abstract void RemoveEquations(EquationSystem system);

    /// <summary>
    /// Get the current position as a Vec2
    /// </summary>
    public abstract Vec2 GetPosition();

    /// <summary>
    /// Move the entity by a delta
    /// </summary>
    public abstract void Move(Vec2 delta);

    /// <summary>
    /// Create a deep clone of this entity
    /// </summary>
    public abstract Entity Clone();

    /// <summary>
    /// Get the display color for rendering
    /// </summary>
    public virtual uint DisplayColor { get; set; } = 0xFFFFFF;
}

/// <summary>
/// Point entity
/// </summary>
public class PointEntity : Entity
{
    public Param X { get; }
    public Param Y { get; }

    public override EntityType Type => EntityType.Point;

    public Vec2 Position
    {
        get => new Vec2(X.value, Y.value);
        set
        {
            X.value = value.X;
            Y.value = value.Y;
        }
    }

    public PointEntity(double x = 0, double y = 0)
    {
        X = new Param("px_" + Id.ToString("N").Substring(0, 8), x);
        Y = new Param("py_" + Id.ToString("N").Substring(0, 8), y);
    }

    public PointEntity(Param x, Param y)
    {
        X = x;
        Y = y;
    }

    public override BBox2d GetBoundingBox() => 
        BBox2d.CreateFromPoints(new Vec2(X.value, Y.value));

    public override double DistanceTo(Vec2 point) => 
        Vec2.Distance(Position, point);

    public override Vec2 ClosestPoint(Vec2 point) => Position;

    public override IEnumerable<Param> GetParams() => new[] { X, Y };

    public override void SetupEquations(EquationSystem system)
    {
        // Point has no defining equations, just parameters
        system.AddParameter(X);
        system.AddParameter(Y);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        system.RemoveParameter(X);
        system.RemoveParameter(Y);
    }

    public override Vec2 GetPosition() => Position;

    public override void Move(Vec2 delta)
    {
        X.value += delta.X;
        Y.value += delta.Y;
    }

    public override Entity Clone() => new PointEntity(X.value, Y.value);

    public override string ToString() => $"Point({X.value:F2}, {Y.value:F2})";
}

/// <summary>
/// Line entity
/// </summary>
public class LineEntity : Entity
{
    public PointEntity Start { get; }
    public PointEntity End { get; }

    public override EntityType Type => EntityType.Line;

    public Vec2 StartPosition
    {
        get => Start.Position;
        set => Start.Position = value;
    }

    public Vec2 EndPosition
    {
        get => End.Position;
        set => End.Position = value;
    }

    public Vec2 Direction => (EndPosition - StartPosition).Normalized;
    public Vec2 Normal => Direction.Perp();
    public double Length => Vec2.Distance(StartPosition, EndPosition);

    public LineEntity(Vec2 start, Vec2 end)
    {
        Start = new PointEntity(start.X, start.Y);
        End = new PointEntity(end.X, end.Y);
    }

    public LineEntity(PointEntity start, PointEntity end)
    {
        Start = start;
        End = end;
    }

    public Vec2 GetPointAt(double t) => Vec2.Lerp(StartPosition, EndPosition, t);

    public override BBox2d GetBoundingBox() => 
        BBox2d.CreateFromPoints(StartPosition, EndPosition);

    public override double DistanceTo(Vec2 point)
    {
        var line = new Line2d(StartPosition, EndPosition);
        return line.DistanceTo(point);
    }

    public override Vec2 ClosestPoint(Vec2 point)
    {
        var line = new Line2d(StartPosition, EndPosition);
        return line.ClosestPoint(point);
    }

    public override IEnumerable<Param> GetParams() => 
        new[] { Start.X, Start.Y, End.X, End.Y };

    public override void SetupEquations(EquationSystem system)
    {
        Start.SetupEquations(system);
        End.SetupEquations(system);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        Start.RemoveEquations(system);
        End.RemoveEquations(system);
    }

    public override Vec2 GetPosition() => StartPosition;

    public override void Move(Vec2 delta)
    {
        Start.Move(delta);
        End.Move(delta);
    }

    public override Entity Clone() => new LineEntity(StartPosition, EndPosition);

    public override string ToString() => $"Line({StartPosition} -> {EndPosition})";
}

/// <summary>
/// Circle entity
/// </summary>
public class CircleEntity : Entity
{
    public PointEntity Center { get; }
    public Param Radius { get; }

    public override EntityType Type => EntityType.Circle;

    public Vec2 CenterPosition
    {
        get => Center.Position;
        set => Center.Position = value;
    }

    public double RadiusValue
    {
        get => Radius.value;
        set => Radius.value = Math.Max(0, value);
    }

    public CircleEntity(Vec2 center, double radius)
    {
        Center = new PointEntity(center.X, center.Y);
        Radius = new Param("r_" + Id.ToString("N").Substring(0, 8), radius);
    }

    public CircleEntity(PointEntity center, Param radius)
    {
        Center = center;
        Radius = radius;
    }

    public Vec2 ClosestPointTo(Vec2 point)
    {
        var direction = (point - CenterPosition).Normalized;
        return CenterPosition + direction * RadiusValue;
    }

    public override BBox2d GetBoundingBox()
    {
        var halfSize = RadiusValue + 1; // Small margin
        return new BBox2d(
            new Vec2(CenterPosition.X - halfSize, CenterPosition.Y - halfSize),
            new Vec2(CenterPosition.X + halfSize, CenterPosition.Y + halfSize));
    }

    public override double DistanceTo(Vec2 point)
    {
        return Math.Abs(Vec2.Distance(point, CenterPosition) - RadiusValue);
    }

    public override Vec2 ClosestPoint(Vec2 point) => ClosestPointTo(point);

    public override IEnumerable<Param> GetParams() => 
        new[] { Center.X, Center.Y, Radius };

    public override void SetupEquations(EquationSystem system)
    {
        Center.SetupEquations(system);
        system.AddParameter(Radius);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        Center.RemoveEquations(system);
        system.RemoveParameter(Radius);
    }

    public override Vec2 GetPosition() => CenterPosition;

    public override void Move(Vec2 delta)
    {
        Center.Move(delta);
    }

    public override Entity Clone() => new CircleEntity(CenterPosition, RadiusValue);

    public override string ToString() => $"Circle(Center={CenterPosition}, R={RadiusValue:F2})";
}

/// <summary>
/// Arc entity
/// </summary>
public class ArcEntity : Entity
{
    public PointEntity Center { get; }
    public Param Radius { get; }
    public Param StartAngle { get; }
    public Param EndAngle { get; }

    public override EntityType Type => EntityType.Arc;

    public Vec2 CenterPosition
    {
        get => Center.Position;
        set => Center.Position = value;
    }

    public double RadiusValue
    {
        get => Radius.value;
        set => Radius.value = Math.Max(0, value);
    }

    public double StartAngleValue
    {
        get => StartAngle.value;
        set => StartAngle.value = value;
    }

    public double EndAngleValue
    {
        get => EndAngle.value;
        set => EndAngle.value = value;
    }

    public Vec2 StartPoint => CenterPosition + new Vec2(
        RadiusValue * Math.Cos(StartAngleValue),
        RadiusValue * Math.Sin(StartAngleValue));

    public Vec2 EndPoint => CenterPosition + new Vec2(
        RadiusValue * Math.Cos(EndAngleValue),
        RadiusValue * Math.Sin(EndAngleValue));

    public double SweepAngle
    {
        get
        {
            var angle = EndAngleValue - StartAngleValue;
            while (angle < 0) angle += 2 * Math.PI;
            while (angle > 2 * Math.PI) angle -= 2 * Math.PI;
            return angle;
        }
    }

    public ArcEntity(Vec2 center, double radius, double startAngle, double endAngle)
    {
        Center = new PointEntity(center.X, center.Y);
        Radius = new Param("ar_" + Id.ToString("N").Substring(0, 8), radius);
        StartAngle = new Param("sa_" + Id.ToString("N").Substring(0, 8), startAngle);
        EndAngle = new Param("ea_" + Id.ToString("N").Substring(0, 8), endAngle);
    }

    public Vec2 GetPointAt(double t) => CenterPosition + new Vec2(
        RadiusValue * Math.Cos(StartAngleValue + t * SweepAngle),
        RadiusValue * Math.Sin(StartAngleValue + t * SweepAngle));

    public override BBox2d GetBoundingBox()
    {
        var bbox = new BBox2d();
        bbox.Expand(StartPoint);
        bbox.Expand(EndPoint);
        // Add intermediate points for better bounding
        for (int i = 1; i < 4; i++)
        {
            bbox.Expand(GetPointAt(i / 4.0));
        }
        return bbox;
    }

    public override double DistanceTo(Vec2 point)
    {
        var closest = ClosestPoint(point);
        return Vec2.Distance(point, closest);
    }

    public override Vec2 ClosestPoint(Vec2 point)
    {
        var toPoint = point - CenterPosition;
        var angle = Math.Atan2(toPoint.Y, toPoint.X);
        
        var start = StartAngleValue;
        var end = EndAngleValue;
        
        while (angle < start) angle += 2 * Math.PI;
        while (angle > start + 2 * Math.PI) angle -= 2 * Math.PI;
        
        if (angle > end)
        {
            var d1 = Vec2.Distance(point, StartPoint);
            var d2 = Vec2.Distance(point, EndPoint);
            return d1 < d2 ? StartPoint : EndPoint;
        }
        
        var direction = new Vec2(Math.Cos(angle), Math.Sin(angle));
        return CenterPosition + direction * RadiusValue;
    }

    public override IEnumerable<Param> GetParams() => 
        new[] { Center.X, Center.Y, Radius, StartAngle, EndAngle };

    public override void SetupEquations(EquationSystem system)
    {
        Center.SetupEquations(system);
        system.AddParameter(Radius);
        system.AddParameter(StartAngle);
        system.AddParameter(EndAngle);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        Center.RemoveEquations(system);
        system.RemoveParameter(Radius);
        system.RemoveParameter(StartAngle);
        system.RemoveParameter(EndAngle);
    }

    public override Vec2 GetPosition() => StartPoint;

    public override void Move(Vec2 delta)
    {
        Center.Move(delta);
    }

    public override Entity Clone() => 
        new ArcEntity(CenterPosition, RadiusValue, StartAngleValue, EndAngleValue);

    public override string ToString() => 
        $"Arc(Center={CenterPosition}, R={RadiusValue:F2}, {StartAngleValue:F2}° -> {EndAngleValue:F2}°)";
}
