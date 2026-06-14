using System;
using System.Collections.Generic;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Constraints;

/// <summary>
/// Arc radius constraint
/// </summary>
public class RadiusConstraint : SingleEntityConstraint
{
    private readonly Exp _radiusExp;
    private readonly Param _radiusParam;

    public double Radius
    {
        get => _radiusParam.value;
        set => _radiusParam.value = value;
    }

    public RadiusConstraint(CircleEntity circle, double radius)
        : base(circle)
    {
        _radiusParam = new Param("r_" + Id.ToString("N").Substring(0, 8), radius);
        _radiusExp = circle.Radius - _radiusParam;
    }

    public RadiusConstraint(ArcEntity arc, double radius)
        : base(arc)
    {
        _radiusParam = new Param("ar_" + Id.ToString("N").Substring(0, 8), radius);
        _radiusExp = arc.Radius - _radiusParam;
    }

    public override string DisplayName => "Radius";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity is CircleEntity circle)
        {
            circle.SetupEquations(system);
        }
        else if (entity is ArcEntity arc)
        {
            arc.SetupEquations(system);
        }
        system.AddParameter(_radiusParam);
        system.AddEquation(_radiusExp);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity is CircleEntity circle)
        {
            circle.RemoveEquations(system);
        }
        else if (entity is ArcEntity arc)
        {
            arc.RemoveEquations(system);
        }
        system.RemoveParameter(_radiusParam);
        system.RemoveEquation(_radiusExp);
    }
}

/// <summary>
/// Equal arc length constraint - two arcs have the same arc length
/// </summary>
public class EqualArcLengthConstraint : TwoEntityConstraint
{
    private readonly Exp _lengthDiff;

    public EqualArcLengthConstraint(ArcEntity arc1, ArcEntity arc2)
        : base(arc1, arc2)
    {
        // Arc length = r * theta (where theta is sweep angle)
        var len1 = arc1.Radius * (arc1.EndAngle - arc1.StartAngle);
        var len2 = arc2.Radius * (arc2.EndAngle - arc2.StartAngle);
        _lengthDiff = len1 - len2;
    }

    public override string DisplayName => "Equal Arc Length";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is ArcEntity a1)
        {
            a1.SetupEquations(system);
        }
        if (entity2 is ArcEntity a2)
        {
            a2.SetupEquations(system);
        }
        system.AddEquation(_lengthDiff);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is ArcEntity a1)
        {
            a1.RemoveEquations(system);
        }
        if (entity2 is ArcEntity a2)
        {
            a2.RemoveEquations(system);
        }
        system.RemoveEquation(_lengthDiff);
    }
}

/// <summary>
/// Arc tangent constraint - arc is tangent to line at point
/// </summary>
public class ArcTangentConstraint : ThreeEntityConstraint
{
    private readonly Exp _tangentEquation;

    public ArcTangentConstraint(ArcEntity arc, LineEntity line, PointEntity tangentPoint)
        : base(arc, line, tangentPoint)
    {
        // At tangent point, arc radius should be perpendicular to line
        // Vector from arc center to tangent point should be perpendicular to line direction
        
        // Line direction
        var dirX = line.End.X - line.Start.X;
        var dirY = line.End.Y - line.Start.Y;
        
        // Radius vector (from center to tangent point)
        var radiusX = tangentPoint.X - arc.Center.X;
        var radiusY = tangentPoint.Y - arc.Center.Y;
        
        // Perpendicular: dot product = 0
        _tangentEquation = dirX * radiusX + dirY * radiusY;
    }

    public override string DisplayName => "Arc Tangent";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is ArcEntity arc)
        {
            arc.SetupEquations(system);
        }
        if (entity2 is LineEntity line)
        {
            line.SetupEquations(system);
        }
        if (entity3 is PointEntity point)
        {
            point.SetupEquations(system);
        }
        system.AddEquation(_tangentEquation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is ArcEntity arc)
        {
            arc.RemoveEquations(system);
        }
        if (entity2 is LineEntity line)
        {
            line.RemoveEquations(system);
        }
        if (entity3 is PointEntity point)
        {
            point.RemoveEquations(system);
        }
        system.RemoveEquation(_tangentEquation);
    }
}

/// <summary>
/// Point on ellipse constraint
/// </summary>
public class PointOnEllipseConstraint : TwoEntityConstraint
{
    private readonly Exp _ellipseEquation;

    public PointOnEllipseConstraint(PointEntity point, EllipseEntity ellipse)
        : base(point, ellipse)
    {
        // Point is on ellipse if: ((x-cx)/rx)^2 + ((y-cy)/ry)^2 = 1
        // In local coordinates: (x'/rx)^2 + (y'/ry)^2 = 1
        var local = ellipse.ToLocal(point.Position);
        var normalizedX = (local.X / ellipse.RadiusXValue);
        var normalizedY = (local.Y / ellipse.RadiusYValue);
        _ellipseEquation = normalizedX * normalizedX + normalizedY * normalizedY - Exp.one;
    }

    public override string DisplayName => "Point On Ellipse";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is PointEntity p)
        {
            p.SetupEquations(system);
        }
        if (entity2 is EllipseEntity ellipse)
        {
            ellipse.SetupEquations(system);
        }
        system.AddEquation(_ellipseEquation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is PointEntity p)
        {
            p.RemoveEquations(system);
        }
        if (entity2 is EllipseEntity ellipse)
        {
            ellipse.RemoveEquations(system);
        }
        system.RemoveEquation(_ellipseEquation);
    }
}

/// <summary>
/// Ellipse tangent constraint
/// </summary>
public class EllipseTangentConstraint : TwoEntityConstraint
{
    private readonly Exp _tangentEquation;

    public EllipseTangentConstraint(LineEntity line, EllipseEntity ellipse)
        : base(line, ellipse)
    {
        // Simplified: distance from line to ellipse center equals semi-minor axis
        // For a more accurate tangent, we'd need to solve for the tangent point
        var lineDirX = line.End.X - line.Start.X;
        var lineDirY = line.End.Y - line.Start.Y;
        
        // Distance from center to line
        var toCenterX = ellipse.Center.X - line.Start.X;
        var toCenterY = ellipse.Center.Y - line.Start.Y;
        
        // Cross product magnitude / line length
        var cross = toCenterX * lineDirY - toCenterY * lineDirX;
        var lineLenSq = lineDirX * lineDirX + lineDirY * lineDirY;
        var distance = Exp.Abs(cross) / Exp.Sqrt(lineLenSq);
        
        // For external tangent, distance equals minor axis
        _tangentEquation = distance - ellipse.RadiusY;
    }

    public override string DisplayName => "Ellipse Tangent";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is LineEntity line)
        {
            line.SetupEquations(system);
        }
        if (entity2 is EllipseEntity ellipse)
        {
            ellipse.SetupEquations(system);
        }
        system.AddEquation(_tangentEquation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is LineEntity line)
        {
            line.RemoveEquations(system);
        }
        if (entity2 is EllipseEntity ellipse)
        {
            ellipse.RemoveEquations(system);
        }
        system.RemoveEquation(_tangentEquation);
    }
}

/// <summary>
/// Concentric ellipse constraint
/// </summary>
public class ConcentricEllipseConstraint : TwoEntityConstraint
{
    private readonly Exp _dx;
    private readonly Exp _dy;

    public ConcentricEllipseConstraint(EllipseEntity ellipse1, EllipseEntity ellipse2)
        : base(ellipse1, ellipse2)
    {
        _dx = ellipse1.Center.X - ellipse2.Center.X;
        _dy = ellipse1.Center.Y - ellipse2.Center.Y;
    }

    public override string DisplayName => "Concentric Ellipse";

    public override int DegreesOfFreedom => 2;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is EllipseEntity e1)
        {
            e1.SetupEquations(system);
        }
        if (entity2 is EllipseEntity e2)
        {
            e2.SetupEquations(system);
        }
        system.AddEquation(_dx);
        system.AddEquation(_dy);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is EllipseEntity e1)
        {
            e1.RemoveEquations(system);
        }
        if (entity2 is EllipseEntity e2)
        {
            e2.RemoveEquations(system);
        }
        system.RemoveEquation(_dx);
        system.RemoveEquation(_dy);
    }
}

/// <summary>
/// Equal ellipse axis constraint
/// </summary>
public class EqualEllipseAxisConstraint : TwoEntityConstraint
{
    private readonly Exp _axisRatioDiff;

    public EqualEllipseAxisConstraint(EllipseEntity ellipse1, EllipseEntity ellipse2)
        : base(ellipse1, ellipse2)
    {
        // Equal aspect ratio
        _axisRatioDiff = ellipse1.RadiusX / ellipse1.RadiusY - ellipse2.RadiusX / ellipse2.RadiusY;
    }

    public override string DisplayName => "Equal Ellipse Axis";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is EllipseEntity e1)
        {
            e1.SetupEquations(system);
        }
        if (entity2 is EllipseEntity e2)
        {
            e2.SetupEquations(system);
        }
        system.AddEquation(_axisRatioDiff);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is EllipseEntity e1)
        {
            e1.RemoveEquations(system);
        }
        if (entity2 is EllipseEntity e2)
        {
            e2.RemoveEquations(system);
        }
        system.RemoveEquation(_axisRatioDiff);
    }
}
