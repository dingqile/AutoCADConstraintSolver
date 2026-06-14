using System;
using System.Collections.Generic;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Entities;

/// <summary>
/// Ellipse entity
/// </summary>
public class EllipseEntity : Entity
{
    public PointEntity Center { get; }
    public Param RadiusX { get; }  // Semi-major axis
    public Param RadiusY { get; }  // Semi-minor axis
    public Param Rotation { get; }  // Rotation angle in radians

    public override EntityType Type => EntityType.Ellipse;

    public Vec2 CenterPosition
    {
        get => Center.Position;
        set => Center.Position = value;
    }

    public double RadiusXValue
    {
        get => RadiusX.value;
        set => RadiusX.value = Math.Max(0, value);
    }

    public double RadiusYValue
    {
        get => RadiusY.value;
        set => RadiusY.value = Math.Max(0, value);
    }

    public double RotationValue
    {
        get => Rotation.value;
        set => Rotation.value = value;
    }

    /// <summary>
    /// Aspect ratio (minor/major)
    /// </summary>
    public double AspectRatio => RadiusYValue / RadiusXValue;

    /// <summary>
    /// Whether this is a valid ellipse (both radii > 0)
    /// </summary>
    public bool IsValid => RadiusXValue > 0 && RadiusYValue > 0;

    public EllipseEntity(Vec2 center, double radiusX, double radiusY, double rotation = 0)
    {
        Center = new PointEntity(center.X, center.Y);
        RadiusX = new Param("ex_" + Id.ToString("N").Substring(0, 8), radiusX);
        RadiusY = new Param("ey_" + Id.ToString("N").Substring(0, 8), radiusY);
        Rotation = new Param("er_" + Id.ToString("N").Substring(0, 8), rotation);
    }

    /// <summary>
    /// Get a point on the ellipse at parameter t (0 to 2π)
    /// </summary>
    public Vec2 GetPointAt(double t)
    {
        var localX = RadiusXValue * Math.Cos(t);
        var localY = RadiusYValue * Math.Sin(t);
        
        // Apply rotation
        var cos = Math.Cos(RotationValue);
        var sin = Math.Sin(RotationValue);
        
        return new Vec2(
            CenterPosition.X + localX * cos - localY * sin,
            CenterPosition.Y + localX * sin + localY * cos);
    }

    /// <summary>
    /// Get the tangent vector at parameter t
    /// </summary>
    public Vec2 GetTangentAt(double t)
    {
        var localX = -RadiusXValue * Math.Sin(t);
        var localY = RadiusYValue * Math.Cos(t);
        
        var cos = Math.Cos(RotationValue);
        var sin = Math.Sin(RotationValue);
        
        return new Vec2(
            localX * cos - localY * sin,
            localX * sin + localY * cos).Normalized;
    }

    /// <summary>
    /// Get the normal vector at parameter t
    /// </summary>
    public Vec2 GetNormalAt(double t)
    {
        var tangent = GetTangentAt(t);
        return tangent.Perp();
    }

    public override BBox2d GetBoundingBox()
    {
        // Approximate bounding box using extreme points
        var bbox = new BBox2d();
        
        for (int i = 0; i < 8; i++)
        {
            bbox.Expand(GetPointAt(i * Math.PI / 4));
        }
        
        return bbox;
    }

    public override double DistanceTo(Vec2 point)
    {
        // Find closest point on ellipse
        var closest = ClosestPoint(point);
        return Vec2.Distance(point, closest);
    }

    public override Vec2 ClosestPoint(Vec2 point)
    {
        // Transform point to local coordinate system
        var local = ToLocal(point);
        
        // Use Newton's method to find closest point
        var t = Math.Atan2(local.Y / RadiusYValue, local.X / RadiusXValue);
        
        for (int i = 0; i < 10; i++)
        {
            var p = new Vec2(RadiusXValue * Math.Cos(t), RadiusYValue * Math.Sin(t));
            var toPoint = local - p;
            var tangent = new Vec2(-RadiusXValue * Math.Sin(t), RadiusYValue * Math.Cos(t));
            
            var denom = Vec2.Dot(tangent, tangent);
            if (Math.Abs(denom) < 1e-10) break;
            
            var dt = Vec2.Dot(toPoint, tangent) / denom;
            t += dt;
            
            if (Math.Abs(dt) < 1e-6) break;
        }
        
        return GetPointAt(t);
    }

    /// <summary>
    /// Transform a point from world coordinates to local ellipse coordinates
    /// </summary>
    public Vec2 ToLocal(Vec2 worldPoint)
    {
        var translated = worldPoint - CenterPosition;
        var cos = Math.Cos(-RotationValue);
        var sin = Math.Sin(-RotationValue);
        
        return new Vec2(
            translated.X * cos - translated.Y * sin,
            translated.X * sin + translated.Y * cos);
    }

    /// <summary>
    /// Transform a point from local coordinates to world coordinates
    /// </summary>
    public Vec2 ToWorld(Vec2 localPoint)
    {
        var cos = Math.Cos(RotationValue);
        var sin = Math.Sin(RotationValue);
        
        return new Vec2(
            CenterPosition.X + localPoint.X * cos - localPoint.Y * sin,
            CenterPosition.Y + localPoint.X * sin + localPoint.Y * cos);
    }

    public override IEnumerable<Param> GetParams() => 
        new[] { Center.X, Center.Y, RadiusX, RadiusY, Rotation };

    public override void SetupEquations(EquationSystem system)
    {
        Center.SetupEquations(system);
        system.AddParameter(RadiusX);
        system.AddParameter(RadiusY);
        system.AddParameter(Rotation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        Center.RemoveEquations(system);
        system.RemoveParameter(RadiusX);
        system.RemoveParameter(RadiusY);
        system.RemoveParameter(Rotation);
    }

    public override Vec2 GetPosition() => CenterPosition;

    public override void Move(Vec2 delta)
    {
        Center.Move(delta);
    }

    public override Entity Clone() => 
        new EllipseEntity(CenterPosition, RadiusXValue, RadiusYValue, RotationValue);

    public override string ToString() => 
        $"Ellipse(Center={CenterPosition}, Rx={RadiusXValue:F2}, Ry={RadiusYValue:F2})";
}

/// <summary>
/// Elliptic arc entity
/// </summary>
public class EllipticArcEntity : Entity
{
    public PointEntity Center { get; }
    public Param RadiusX { get; }
    public Param RadiusY { get; }
    public Param Rotation { get; }
    public Param StartAngle { get; }
    public Param EndAngle { get; }

    public override EntityType Type => EntityType.EllipticArc;

    public Vec2 CenterPosition
    {
        get => Center.Position;
        set => Center.Position = value;
    }

    public double RadiusXValue
    {
        get => RadiusX.value;
        set => RadiusX.value = Math.Max(0, value);
    }

    public double RadiusYValue
    {
        get => RadiusY.value;
        set => RadiusY.value = Math.Max(0, value);
    }

    public double RotationValue
    {
        get => Rotation.value;
        set => Rotation.value = value;
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

    public Vec2 StartPoint
    {
        get
        {
            var local = new Vec2(
                RadiusXValue * Math.Cos(StartAngleValue),
                RadiusYValue * Math.Sin(StartAngleValue));
            return ToWorld(local);
        }
    }

    public Vec2 EndPoint
    {
        get
        {
            var local = new Vec2(
                RadiusXValue * Math.Cos(EndAngleValue),
                RadiusYValue * Math.Sin(EndAngleValue));
            return ToWorld(local);
        }
    }

    public EllipticArcEntity(Vec2 center, double radiusX, double radiusY, 
        double rotation, double startAngle, double endAngle)
    {
        Center = new PointEntity(center.X, center.Y);
        RadiusX = new Param("eax_" + Id.ToString("N").Substring(0, 8), radiusX);
        RadiusY = new Param("eay_" + Id.ToString("N").Substring(0, 8), radiusY);
        Rotation = new Param("ear_" + Id.ToString("N").Substring(0, 8), rotation);
        StartAngle = new Param("easa_" + Id.ToString("N").Substring(0, 8), startAngle);
        EndAngle = new Param("eaea_" + Id.ToString("N").Substring(0, 8), endAngle);
    }

    private Vec2 ToWorld(Vec2 local)
    {
        var cos = Math.Cos(RotationValue);
        var sin = Math.Sin(RotationValue);
        
        return new Vec2(
            CenterPosition.X + local.X * cos - local.Y * sin,
            CenterPosition.Y + local.X * sin + local.Y * cos);
    }

    public Vec2 GetPointAt(double t) => 
        ToWorld(new Vec2(
            RadiusXValue * Math.Cos(StartAngleValue + t * SweepAngle),
            RadiusYValue * Math.Sin(StartAngleValue + t * SweepAngle)));

    public override BBox2d GetBoundingBox()
    {
        var bbox = new BBox2d();
        bbox.Expand(StartPoint);
        bbox.Expand(EndPoint);
        
        // Add intermediate points for better bounding
        for (int i = 1; i < 8; i++)
        {
            bbox.Expand(GetPointAt(i / 8.0));
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
        // Simplified: find closest point on full ellipse, then clamp to arc
        var local = point - CenterPosition;
        var cos = Math.Cos(-RotationValue);
        var sin = Math.Sin(-RotationValue);
        var localRotated = new Vec2(
            local.X * cos - local.Y * sin,
            local.X * sin + local.Y * cos);

        var t = Math.Atan2(localRotated.Y / RadiusYValue, localRotated.X / RadiusXValue);
        
        // Clamp to arc range
        while (t < StartAngleValue) t += 2 * Math.PI;
        while (t > StartAngleValue + 2 * Math.PI) t -= 2 * Math.PI;
        
        if (t > EndAngleValue)
        {
            var d1 = Vec2.Distance(point, StartPoint);
            var d2 = Vec2.Distance(point, EndPoint);
            return d1 < d2 ? StartPoint : EndPoint;
        }

        return GetPointAt((t - StartAngleValue) / SweepAngle);
    }

    public override IEnumerable<Param> GetParams() => 
        new[] { Center.X, Center.Y, RadiusX, RadiusY, Rotation, StartAngle, EndAngle };

    public override void SetupEquations(EquationSystem system)
    {
        Center.SetupEquations(system);
        system.AddParameter(RadiusX);
        system.AddParameter(RadiusY);
        system.AddParameter(Rotation);
        system.AddParameter(StartAngle);
        system.AddParameter(EndAngle);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        Center.RemoveEquations(system);
        system.RemoveParameter(RadiusX);
        system.RemoveParameter(RadiusY);
        system.RemoveParameter(Rotation);
        system.RemoveParameter(StartAngle);
        system.RemoveParameter(EndAngle);
    }

    public override Vec2 GetPosition() => StartPoint;

    public override void Move(Vec2 delta)
    {
        Center.Move(delta);
    }

    public override Entity Clone() => 
        new EllipticArcEntity(CenterPosition, RadiusXValue, RadiusYValue, 
            RotationValue, StartAngleValue, EndAngleValue);

    public override string ToString() => 
        $"EllipticArc(Center={CenterPosition}, Rx={RadiusXValue:F2}, Ry={RadiusYValue:F2})";
}
