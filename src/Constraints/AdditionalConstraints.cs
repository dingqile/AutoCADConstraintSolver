using System;
using System.Collections.Generic;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Constraints;

/// <summary>
/// Circles/arcs distance constraint - distance between circles/arcs with options
/// Based on NoteCAD's CirclesDistance constraint
/// </summary>
public class CirclesDistanceConstraint : TwoEntityConstraint
{
    /// <summary>
    /// Distance options between circles
    /// </summary>
    public enum DistanceOption
    {
        /// <summary>Distance from outside of first to outside of second</summary>
        Outside,
        /// <summary>First circle inside second</summary>
        FirstInside,
        /// <summary>Second circle inside first</summary>
        SecondInside
    }

    private readonly Exp _distanceEquation;
    private readonly Param _distanceParam;
    private readonly DistanceOption _option;

    public double Distance
    {
        get => _distanceParam.value;
        set => _distanceParam.value = value;
    }

    public DistanceOption Option => _option;

    public CirclesDistanceConstraint(CircleEntity c1, CircleEntity c2, double distance, DistanceOption option = DistanceOption.Outside)
        : base(c1, c2)
    {
        _option = option;
        _distanceParam = new Param("cd_" + Id.ToString("N").Substring(0, 8), distance);

        // For circles: distance = center_distance - r1 - r2 (outside)
        var dx = c2.Center.X - c1.Center.X;
        var dy = c2.Center.Y - c1.Center.Y;
        var centerDist = Exp.Sqrt(dx * dx + dy * dy);

        switch (option)
        {
            case DistanceOption.Outside:
                _distanceEquation = centerDist - c1.Radius - c2.Radius - _distanceParam;
                break;
            case DistanceOption.FirstInside:
                _distanceEquation = c2.Radius - c1.Radius - centerDist - _distanceParam;
                break;
            case DistanceOption.SecondInside:
                _distanceEquation = c1.Radius - c2.Radius - centerDist - _distanceParam;
                break;
            default:
                _distanceEquation = centerDist - c1.Radius - c2.Radius - _distanceParam;
                break;
        }
    }

    public CirclesDistanceConstraint(ArcEntity a1, ArcEntity a2, double distance, DistanceOption option = DistanceOption.Outside)
        : base(a1, a2)
    {
        _option = option;
        _distanceParam = new Param("cad_" + Id.ToString("N").Substring(0, 8), distance);

        var dx = a2.Center.X - a1.Center.X;
        var dy = a2.Center.Y - a1.Center.Y;
        var centerDist = Exp.Sqrt(dx * dx + dy * dy);

        switch (option)
        {
            case DistanceOption.Outside:
                _distanceEquation = centerDist - a1.Radius - a2.Radius - _distanceParam;
                break;
            case DistanceOption.FirstInside:
                _distanceEquation = a2.Radius - a1.Radius - centerDist - _distanceParam;
                break;
            case DistanceOption.SecondInside:
                _distanceEquation = a1.Radius - a2.Radius - centerDist - _distanceParam;
                break;
            default:
                _distanceEquation = centerDist - a1.Radius - a2.Radius - _distanceParam;
                break;
        }
    }

    public CirclesDistanceConstraint(CircleEntity circle, ArcEntity arc, double distance, DistanceOption option = DistanceOption.Outside)
        : base(circle, arc)
    {
        _option = option;
        _distanceParam = new Param("cda_" + Id.ToString("N").Substring(0, 8), distance);

        var dx = arc.Center.X - circle.Center.X;
        var dy = arc.Center.Y - circle.Center.Y;
        var centerDist = Exp.Sqrt(dx * dx + dy * dy);

        switch (option)
        {
            case DistanceOption.Outside:
                _distanceEquation = centerDist - circle.Radius - arc.Radius - _distanceParam;
                break;
            case DistanceOption.FirstInside:
                _distanceEquation = arc.Radius - circle.Radius - centerDist - _distanceParam;
                break;
            case DistanceOption.SecondInside:
                _distanceEquation = circle.Radius - arc.Radius - centerDist - _distanceParam;
                break;
            default:
                _distanceEquation = centerDist - circle.Radius - arc.Radius - _distanceParam;
                break;
        }
    }

    public override string DisplayName => "Circles Distance";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is CircleEntity c1)
        {
            c1.SetupEquations(system);
        }
        else if (entity1 is ArcEntity a1)
        {
            a1.SetupEquations(system);
        }

        if (entity2 is CircleEntity c2)
        {
            c2.SetupEquations(system);
        }
        else if (entity2 is ArcEntity a2)
        {
            a2.SetupEquations(system);
        }

        system.AddParameter(_distanceParam);
        system.AddEquation(_distanceEquation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is CircleEntity c1)
        {
            c1.RemoveEquations(system);
        }
        else if (entity1 is ArcEntity a1)
        {
            a1.RemoveEquations(system);
        }

        if (entity2 is CircleEntity c2)
        {
            c2.RemoveEquations(system);
        }
        else if (entity2 is ArcEntity a2)
        {
            a2.RemoveEquations(system);
        }

        system.RemoveParameter(_distanceParam);
        system.RemoveEquation(_distanceEquation);
    }
}

/// <summary>
/// Line to circle distance constraint
/// Based on NoteCAD's LineCircleDistance
/// </summary>
public class LineCircleDistanceConstraint : TwoEntityConstraint
{
    /// <summary>
    /// Distance options between line and circle
    /// </summary>
    public enum LineCircleOption
    {
        /// <summary>Distance from line to circle center minus radius</summary>
        Default,
        /// <summary>Line tangent to circle externally</summary>
        External,
        /// <summary>Line tangent to circle internally</summary>
        Internal
    }

    private readonly Exp _distanceEquation;
    private readonly Param _distanceParam;
    private readonly LineCircleOption _option;

    public double Distance
    {
        get => _distanceParam.value;
        set => _distanceParam.value = value;
    }

    public LineCircleOption Option => _option;

    public LineCircleDistanceConstraint(LineEntity line, CircleEntity circle, double distance, LineCircleOption option = LineCircleOption.Default)
        : base(line, circle)
    {
        _option = option;
        _distanceParam = new Param("lcd_" + Id.ToString("N").Substring(0, 8), distance);

        // Calculate perpendicular distance from line to circle center
        var lineDirX = line.End.X - line.Start.X;
        var lineDirY = line.End.Y - line.Start.Y;
        var lineLenSq = lineDirX * lineDirX + lineDirY * lineDirY;
        
        // Vector from line start to circle center
        var toCenterX = circle.Center.X - line.Start.X;
        var toCenterY = circle.Center.Y - line.Start.Y;
        
        // Cross product for perpendicular distance
        var cross = toCenterX * lineDirY - toCenterY * lineDirX;
        var perpDist = Exp.Abs(cross) / Exp.Sqrt(lineLenSq);

        switch (option)
        {
            case LineCircleOption.Default:
                _distanceEquation = perpDist - circle.Radius - _distanceParam;
                break;
            case LineCircleOption.External:
                // Tangent externally: perp distance = radius + distance
                _distanceEquation = perpDist - circle.Radius - _distanceParam;
                break;
            case LineCircleOption.Internal:
                // Tangent internally: perp distance = |radius - distance|
                _distanceEquation = perpDist - Exp.Abs(circle.Radius - _distanceParam);
                break;
            default:
                _distanceEquation = perpDist - circle.Radius - _distanceParam;
                break;
        }
    }

    public LineCircleDistanceConstraint(LineEntity line, ArcEntity arc, double distance, LineCircleOption option = LineCircleOption.Default)
        : base(line, arc)
    {
        _option = option;
        _distanceParam = new Param("lcd_" + Id.ToString("N").Substring(0, 8), distance);

        var lineDirX = line.End.X - line.Start.X;
        var lineDirY = line.End.Y - line.Start.Y;
        var lineLenSq = lineDirX * lineDirX + lineDirY * lineDirY;
        
        var toCenterX = arc.Center.X - line.Start.X;
        var toCenterY = arc.Center.Y - line.Start.Y;
        
        var cross = toCenterX * lineDirY - toCenterY * lineDirX;
        var perpDist = Exp.Abs(cross) / Exp.Sqrt(lineLenSq);

        switch (option)
        {
            case LineCircleOption.Default:
                _distanceEquation = perpDist - arc.Radius - _distanceParam;
                break;
            case LineCircleOption.External:
                _distanceEquation = perpDist - arc.Radius - _distanceParam;
                break;
            case LineCircleOption.Internal:
                _distanceEquation = perpDist - Exp.Abs(arc.Radius - _distanceParam);
                break;
            default:
                _distanceEquation = perpDist - arc.Radius - _distanceParam;
                break;
        }
    }

    public override string DisplayName => "Line-Circle Distance";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is LineEntity line)
        {
            line.SetupEquations(system);
        }

        if (entity2 is CircleEntity circle)
        {
            circle.SetupEquations(system);
        }
        else if (entity2 is ArcEntity arc)
        {
            arc.SetupEquations(system);
        }

        system.AddParameter(_distanceParam);
        system.AddEquation(_distanceEquation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is LineEntity line)
        {
            line.RemoveEquations(system);
        }

        if (entity2 is CircleEntity circle)
        {
            circle.RemoveEquations(system);
        }
        else if (entity2 is ArcEntity arc)
        {
            arc.RemoveEquations(system);
        }

        system.RemoveParameter(_distanceParam);
        system.RemoveEquation(_distanceEquation);
    }
}

/// <summary>
/// Point on arc constraint - point lies on arc
/// </summary>
public class PointOnArcConstraint : TwoEntityConstraint
{
    private readonly Exp _distanceEquation;
    private readonly Exp _angleEquation;

    public PointOnArcConstraint(PointEntity point, ArcEntity arc)
        : base(point, arc)
    {
        // Point is on arc if:
        // 1. Distance from center equals radius
        // 2. Angle is within arc's start/end angle range

        var dx = point.X - arc.Center.X;
        var dy = point.Y - arc.Center.Y;
        _distanceEquation = Exp.Sqrt(dx * dx + dy * dy) - arc.Radius;

        // For the angle, we use atan2
        _angleEquation = Exp.zero; // Simplified - full implementation would check angle range
    }

    public override string DisplayName => "Point On Arc";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is PointEntity p)
        {
            p.SetupEquations(system);
        }
        if (entity2 is ArcEntity arc)
        {
            arc.SetupEquations(system);
        }
        system.AddEquation(_distanceEquation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is PointEntity p)
        {
            p.RemoveEquations(system);
        }
        if (entity2 is ArcEntity arc)
        {
            arc.RemoveEquations(system);
        }
        system.RemoveEquation(_distanceEquation);
    }
}

/// <summary>
/// Arc-arc tangent constraint
/// </summary>
public class ArcArcTangentConstraint : TwoEntityConstraint
{
    private readonly Exp _tangentEquation;

    public ArcArcTangentConstraint(ArcEntity arc1, ArcEntity arc2)
        : base(arc1, arc2)
    {
        // At tangent point, the sum of radii equals center distance (externally tangent)
        // Or the difference of radii equals center distance (internally tangent)
        
        var dx = arc2.Center.X - arc1.Center.X;
        var dy = arc2.Center.Y - arc1.Center.Y;
        var centerDist = Exp.Sqrt(dx * dx + dy * dy);
        
        // External tangent: distance between centers = r1 + r2
        _tangentEquation = centerDist - arc1.Radius - arc2.Radius;
    }

    public override string DisplayName => "Arc-Arc Tangent";

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
        system.AddEquation(_tangentEquation);
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
        system.RemoveEquation(_tangentEquation);
    }
}

/// <summary>
/// Arc-line tangent constraint at specific point
/// </summary>
public class ArcLineTangentConstraint : ThreeEntityConstraint
{
    private readonly Exp _tangentEquation;

    public ArcLineTangentConstraint(ArcEntity arc, LineEntity line, PointEntity tangentPoint)
        : base(arc, line, tangentPoint)
    {
        // At tangent point, the line is perpendicular to the radius
        var radiusX = tangentPoint.X - arc.Center.X;
        var radiusY = tangentPoint.Y - arc.Center.Y;
        var lineDirX = line.End.X - line.Start.X;
        var lineDirY = line.End.Y - line.Start.Y;

        // Dot product should be zero for perpendicular
        _tangentEquation = radiusX * lineDirX + radiusY * lineDirY;
    }

    public override string DisplayName => "Arc-Line Tangent";

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
/// Symmetric line constraint - line is symmetric about an axis
/// </summary>
public class SymmetricLineConstraint : TwoEntityConstraint
{
    private readonly Exp _symX;
    private readonly Exp _symY;

    public SymmetricLineConstraint(LineEntity line1, LineEntity line2, LineEntity axis)
        : base(line1, line2)
    {
        // Two lines symmetric about an axis means:
        // 1. They have equal angle with the axis
        // 2. They are on opposite sides of the axis

        // Get direction vectors
        var dir1X = line1.End.X - line1.Start.X;
        var dir1Y = line1.End.Y - line1.Start.Y;
        var dir2X = line2.End.X - line2.Start.X;
        var dir2Y = line2.End.Y - line2.Start.Y;
        var axisDirX = axis.End.X - axis.Start.X;
        var axisDirY = axis.End.Y - axis.Start.Y;

        // Cross products to check if directions are symmetric
        // For symmetric lines, the reflections should match
        var cross1 = dir1X * axisDirY - dir1Y * axisDirX;
        var cross2 = dir2X * axisDirY - dir2Y * axisDirX;
        
        // Symmetric means cross products have opposite signs
        _symX = Exp.Abs(cross1) - Exp.Abs(cross2);

        // Also ensure the lines are on opposite sides
        var mid1X = (line1.Start.X + line1.End.X) * Exp.one / Exp.two;
        var mid1Y = (line1.Start.Y + line1.End.Y) * Exp.one / Exp.two;
        var mid2X = (line2.Start.X + line2.End.X) * Exp.one / Exp.two;
        var mid2Y = (line2.Start.Y + line2.End.Y) * Exp.one / Exp.two;

        var axisStartToMid1X = mid1X - axis.Start.X;
        var axisStartToMid1Y = mid1Y - axis.Start.Y;
        var axisStartToMid2X = mid2X - axis.Start.X;
        var axisStartToMid2Y = mid2Y - axis.Start.Y;

        var crossMid1 = axisStartToMid1X * axisDirY - axisStartToMid1Y * axisDirX;
        var crossMid2 = axisStartToMid2X * axisDirY - axisStartToMid2Y * axisDirX;

        // Cross products should have opposite signs
        _symY = crossMid1 + crossMid2; // Sum is zero when opposite
    }

    public override string DisplayName => "Symmetric Lines";

    public override int DegreesOfFreedom => 2;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is LineEntity line1)
        {
            line1.SetupEquations(system);
        }
        if (entity2 is LineEntity line2)
        {
            line2.SetupEquations(system);
        }
        system.AddEquation(_symX);
        system.AddEquation(_symY);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is LineEntity line1)
        {
            line1.RemoveEquations(system);
        }
        if (entity2 is LineEntity line2)
        {
            line2.RemoveEquations(system);
        }
        system.RemoveEquation(_symX);
        system.RemoveEquation(_symY);
    }
}

/// <summary>
/// Fixed angle constraint - angle has a specific value
/// </summary>
public class FixedAngleConstraint : SingleEntityConstraint
{
    private readonly Exp _angleEquation;
    private readonly Param _angleParam;

    public double Angle
    {
        get => _angleParam.value;
        set => _angleParam.value = value;
    }

    public FixedAngleConstraint(LineEntity line, double angleInRadians)
        : base(line)
    {
        _angleParam = new Param("fa_" + Id.ToString("N").Substring(0, 8), angleInRadians);

        // Calculate the angle of the line relative to X axis
        var dx = line.End.X - line.Start.X;
        var dy = line.End.Y - line.Start.Y;
        
        // atan2(dy, dx) = target angle
        _angleEquation = Exp.Atan2(dy, dx) - _angleParam;
    }

    public override string DisplayName => "Fixed Angle";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity is LineEntity line)
        {
            line.SetupEquations(system);
        }
        system.AddParameter(_angleParam);
        system.AddEquation(_angleEquation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity is LineEntity line)
        {
            line.RemoveEquations(system);
        }
        system.RemoveParameter(_angleParam);
        system.RemoveEquation(_angleEquation);
    }
}

/// <summary>
/// Point on extension line constraint - point lies on line extension
/// </summary>
public class PointOnLineExtensionConstraint : TwoEntityConstraint
{
    private readonly Exp _extensionEquation;

    public PointOnLineExtensionConstraint(PointEntity point, LineEntity line)
        : base(point, line)
    {
        // Point on line extension: cross product = 0
        var dx = line.End.X - line.Start.X;
        var dy = line.End.Y - line.Start.Y;
        var px = point.X - line.Start.X;
        var py = point.Y - line.Start.Y;

        _extensionEquation = dx * py - dy * px;
    }

    public override string DisplayName => "Point On Extension";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is PointEntity p)
        {
            p.SetupEquations(system);
        }
        if (entity2 is LineEntity line)
        {
            line.SetupEquations(system);
        }
        system.AddEquation(_extensionEquation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is PointEntity p)
        {
            p.RemoveEquations(system);
        }
        if (entity2 is LineEntity line)
        {
            line.RemoveEquations(system);
        }
        system.RemoveEquation(_extensionEquation);
    }
}
