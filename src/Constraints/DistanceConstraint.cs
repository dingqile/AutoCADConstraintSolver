using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Constraints;

/// <summary>
/// Distance constraint between two points
/// </summary>
public class PointsDistanceConstraint : TwoEntityConstraint
{
    private readonly Exp _distanceExp;
    private readonly Param _distanceParam;

    public double Distance
    {
        get => _distanceParam.value;
        set => _distanceParam.value = value;
    }

    public PointsDistanceConstraint(PointEntity point1, PointEntity point2, double distance)
        : base(point1, point2)
    {
        _distanceParam = new Param("d_" + Id.ToString("N").Substring(0, 8), distance);
        
        // Distance equation: sqrt((x2-x1)^2 + (y2-y1)^2) = d
        var dx = point2.X - point1.X;
        var dy = point2.Y - point1.Y;
        _distanceExp = Exp.Sqrt(dx * dx + dy * dy) - _distanceParam;
    }

    public override string DisplayName => "Distance";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is PointEntity p1)
        {
            p1.SetupEquations(system);
        }
        if (entity2 is PointEntity p2)
        {
            p2.SetupEquations(system);
        }
        system.AddParameter(_distanceParam);
        system.AddEquation(_distanceExp);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is PointEntity p1)
        {
            p1.RemoveEquations(system);
        }
        if (entity2 is PointEntity p2)
        {
            p2.RemoveEquations(system);
        }
        system.RemoveParameter(_distanceParam);
        system.RemoveEquation(_distanceExp);
    }
}

/// <summary>
/// Distance constraint between a point and a line
/// </summary>
public class PointLineDistanceConstraint : TwoEntityConstraint
{
    private readonly Exp _distanceExp;
    private readonly Param _distanceParam;

    public double Distance
    {
        get => _distanceParam.value;
        set => _distanceParam.value = value;
    }

    public PointLineDistanceConstraint(PointEntity point, LineEntity line, double distance)
        : base(point, line)
    {
        _distanceParam = new Param("pld_" + Id.ToString("N").Substring(0, 8), distance);
        
        // Distance from point to line using cross product formula
        var dx = line.End.X - line.Start.X;
        var dy = line.End.Y - line.Start.Y;
        var lenSq = dx * dx + dy * dy;
        
        // Cross product magnitude / line length
        var cross = (point.X - line.Start.X) * dy - (point.Y - line.Start.Y) * dx;
        _distanceExp = Exp.Abs(cross) / Exp.Sqrt(lenSq) - _distanceParam;
    }

    public override string DisplayName => "Point-Line Distance";

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
        system.AddParameter(_distanceParam);
        system.AddEquation(_distanceExp);
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
        system.RemoveParameter(_distanceParam);
        system.RemoveEquation(_distanceExp);
    }
}

/// <summary>
/// Distance constraint between two lines (perpendicular distance)
/// </summary>
public class LineLineDistanceConstraint : TwoEntityConstraint
{
    private readonly Exp _distanceExp;
    private readonly Param _distanceParam;

    public double Distance
    {
        get => _distanceParam.value;
        set => _distanceParam.value = value;
    }

    public LineLineDistanceConstraint(LineEntity line1, LineEntity line2, double distance)
        : base(line1, line2)
    {
        _distanceParam = new Param("lld_" + Id.ToString("N").Substring(0, 8), distance);
        
        // Calculate perpendicular distance between parallel lines
        // For non-parallel lines, this will be a variable constraint
        var dx = line2.Start.X - line1.Start.X;
        var dy = line2.Start.Y - line1.Start.Y;
        
        // Get direction of first line
        var dirX = line1.End.X - line1.Start.X;
        var dirY = line1.End.Y - line1.Start.Y;
        var lenSq = dirX * dirX + dirY * dirY;
        
        // Perpendicular distance using cross product
        var cross = dx * dirY - dy * dirX;
        _distanceExp = Exp.Abs(cross) / Exp.Sqrt(lenSq) - _distanceParam;
    }

    public override string DisplayName => "Line-Line Distance";

    public override int DegreesOfFreedom => 1;

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
        system.AddParameter(_distanceParam);
        system.AddEquation(_distanceExp);
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
        system.RemoveParameter(_distanceParam);
        system.RemoveEquation(_distanceExp);
    }
}

/// <summary>
/// Angle constraint between two lines
/// </summary>
public class AngleConstraint : TwoEntityConstraint
{
    private readonly Exp _angleExp;
    private readonly Param _angleParam;

    public double Angle
    {
        get => _angleParam.value;
        set => _angleParam.value = value;
    }

    public bool IsReflex { get; set; }

    public AngleConstraint(LineEntity line1, LineEntity line2, double angleInDegrees)
        : base(line1, line2)
    {
        _angleParam = new Param("ang_" + Id.ToString("N").Substring(0, 8), 
            angleInDegrees * Math.PI / 180.0);
        
        // Calculate angle between two lines using atan2 of cross/dot products
        var dx1 = line1.End.X - line1.Start.X;
        var dy1 = line1.End.Y - line1.Start.Y;
        var dx2 = line2.End.X - line2.Start.X;
        var dy2 = line2.End.Y - line2.Start.Y;
        
        var cross = dx1 * dy2 - dy1 * dx2;
        var dot = dx1 * dx2 + dy1 * dy2;
        
        _angleExp = Exp.Atan2(Exp.Abs(cross), dot) - _angleParam;
    }

    public override string DisplayName => "Angle";

    public override int DegreesOfFreedom => 1;

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
        system.AddParameter(_angleParam);
        system.AddEquation(_angleExp);
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
        system.RemoveParameter(_angleParam);
        system.RemoveEquation(_angleExp);
    }
}

/// <summary>
/// Line length constraint
/// </summary>
public class LengthConstraint : SingleEntityConstraint
{
    private readonly Exp _lengthExp;
    private readonly Param _lengthParam;

    public double Length
    {
        get => _lengthParam.value;
        set => _lengthParam.value = value;
    }

    public LengthConstraint(LineEntity line, double length)
        : base(line)
    {
        _lengthParam = new Param("len_" + Id.ToString("N").Substring(0, 8), length);
        
        var dx = line.End.X - line.Start.X;
        var dy = line.End.Y - line.Start.Y;
        _lengthExp = Exp.Sqrt(dx * dx + dy * dy) - _lengthParam;
    }

    public override string DisplayName => "Length";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity is LineEntity line)
        {
            line.SetupEquations(system);
        }
        system.AddParameter(_lengthParam);
        system.AddEquation(_lengthExp);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity is LineEntity line)
        {
            line.RemoveEquations(system);
        }
        system.RemoveParameter(_lengthParam);
        system.RemoveEquation(_lengthExp);
    }
}

/// <summary>
/// Circle diameter constraint
/// </summary>
public class DiameterConstraint : SingleEntityConstraint
{
    private readonly Exp _diameterExp;
    private readonly Param _diameterParam;

    public double Diameter
    {
        get => _diameterParam.value;
        set => _diameterParam.value = value;
    }

    public DiameterConstraint(CircleEntity circle, double diameter)
        : base(circle)
    {
        _diameterParam = new Param("dia_" + Id.ToString("N").Substring(0, 8), diameter);
        _diameterExp = Exp.one / Exp.two * circle.Radius - _diameterParam;
    }

    public override string DisplayName => "Diameter";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity is CircleEntity circle)
        {
            circle.SetupEquations(system);
        }
        system.AddParameter(_diameterParam);
        system.AddEquation(_diameterExp);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity is CircleEntity circle)
        {
            circle.RemoveEquations(system);
        }
        system.RemoveParameter(_diameterParam);
        system.RemoveEquation(_diameterExp);
    }
}

/// <summary>
/// Point on entity constraint
/// </summary>
public class PointOnEntityConstraint : TwoEntityConstraint
{
    private readonly Exp _equation;

    public PointOnEntityConstraint(PointEntity point, LineEntity line)
        : base(point, line)
    {
        // Calculate if point lies on line using cross product
        var dx = line.End.X - line.Start.X;
        var dy = line.End.Y - line.Start.Y;
        var cross = (point.X - line.Start.X) * dy - (point.Y - line.Start.Y) * dx;
        _equation = cross;
    }

    public PointOnEntityConstraint(PointEntity point, CircleEntity circle)
        : base(point, circle)
    {
        // Distance from point to circle center should equal radius
        var dx = point.X - circle.Center.X;
        var dy = point.Y - circle.Center.Y;
        _equation = Exp.Sqrt(dx * dx + dy * dy) - circle.Radius;
    }

    public PointOnEntityConstraint(PointEntity point, ArcEntity arc)
        : base(point, arc)
    {
        // Distance from point to arc center should equal radius
        var dx = point.X - arc.Center.X;
        var dy = point.Y - arc.Center.Y;
        _equation = Exp.Sqrt(dx * dx + dy * dy) - arc.Radius;
    }

    public override string DisplayName => "Point On";

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
        else if (entity2 is CircleEntity circle)
        {
            circle.SetupEquations(system);
        }
        else if (entity2 is ArcEntity arc)
        {
            arc.SetupEquations(system);
        }
        system.AddEquation(_equation);
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
        else if (entity2 is CircleEntity circle)
        {
            circle.RemoveEquations(system);
        }
        else if (entity2 is ArcEntity arc)
        {
            arc.RemoveEquations(system);
        }
        system.RemoveEquation(_equation);
    }
}
