using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Constraints;

/// <summary>
/// Coincident constraint - two points must be at the same location
/// </summary>
public class CoincidentConstraint : TwoEntityConstraint
{
    private readonly ExpVector2d _diffX;
    private readonly ExpVector2d _diffY;
    private readonly EqExp _eqX;
    private readonly EqExp _eqY;

    public CoincidentConstraint(PointEntity point1, PointEntity point2)
        : base(point1, point2)
    {
        // Create equality equations for X and Y coordinates
        _eqX = new EqExp(point1.X, point2.X);
        _eqY = new EqExp(point1.Y, point2.Y);
        _diffX = new ExpVector2d(_eqX, Exp.zero);
        _diffY = new ExpVector2d(Exp.zero, _eqY);
    }

    public override string DisplayName => "Coincident";

    public override int DegreesOfFreedom => 2;

    public override IEnumerable<Entity> GetEntities()
    {
        if (entity1 is PointEntity p1 && entity2 is PointEntity p2)
        {
            return new[] { p1, p2 };
        }
        return base.GetEntities();
    }

    public override void SetupEquations(EquationSystem system)
    {
        // Add the point coordinates to the system if needed
        if (entity1 is PointEntity p1)
        {
            p1.SetupEquations(system);
        }
        if (entity2 is PointEntity p2)
        {
            p2.SetupEquations(system);
        }
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
    }
}

/// <summary>
/// Horizontal constraint - a line should be horizontal
/// </summary>
public class HorizontalConstraint : SingleEntityConstraint
{
    private readonly Exp _dx;
    private readonly Exp _dy;

    public HorizontalConstraint(LineEntity line)
        : base(line)
    {
        // For a horizontal line, dy = 0 (the Y difference should be zero)
        _dx = line.End.Y - line.Start.Y;
        _dy = Exp.zero;
    }

    public override string DisplayName => "Horizontal";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity is LineEntity line)
        {
            line.SetupEquations(system);
            system.AddEquation(_dx);
        }
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity is LineEntity line)
        {
            line.RemoveEquations(system);
            system.RemoveEquation(_dx);
        }
    }
}

/// <summary>
/// Vertical constraint - a line should be vertical
/// </summary>
public class VerticalConstraint : SingleEntityConstraint
{
    private readonly Exp _dx;

    public VerticalConstraint(LineEntity line)
        : base(line)
    {
        // For a vertical line, dx = 0 (the X difference should be zero)
        _dx = line.End.X - line.Start.X;
    }

    public override string DisplayName => "Vertical";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity is LineEntity line)
        {
            line.SetupEquations(system);
            system.AddEquation(_dx);
        }
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity is LineEntity line)
        {
            line.RemoveEquations(system);
            system.RemoveEquation(_dx);
        }
    }
}

/// <summary>
/// Parallel constraint - two lines should be parallel
/// </summary>
public class ParallelConstraint : TwoEntityConstraint
{
    private readonly Exp _crossProduct;

    public ParallelConstraint(LineEntity line1, LineEntity line2)
        : base(line1, line2)
    {
        // For parallel lines, cross product of directions should be zero
        var dir1 = new ExpVector2d(line1.End.X - line1.Start.X, line1.End.Y - line1.Start.Y);
        var dir2 = new ExpVector2d(line2.End.X - line2.Start.X, line2.End.Y - line2.Start.Y);
        _crossProduct = ExpVector2d.Cross(dir1, dir2);
    }

    public override string DisplayName => "Parallel";

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
        system.AddEquation(_crossProduct);
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
        system.RemoveEquation(_crossProduct);
    }
}

/// <summary>
/// Perpendicular constraint - two lines should be perpendicular
/// </summary>
public class PerpendicularConstraint : TwoEntityConstraint
{
    private readonly Exp _dotProduct;

    public PerpendicularConstraint(LineEntity line1, LineEntity line2)
        : base(line1, line2)
    {
        // For perpendicular lines, dot product of directions should be zero
        var dir1 = new ExpVector2d(line1.End.X - line1.Start.X, line1.End.Y - line1.Start.Y);
        var dir2 = new ExpVector2d(line2.End.X - line2.Start.X, line2.End.Y - line2.Start.Y);
        _dotProduct = ExpVector2d.Dot(dir1, dir2);
    }

    public override string DisplayName => "Perpendicular";

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
        system.AddEquation(_dotProduct);
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
        system.RemoveEquation(_dotProduct);
    }
}

/// <summary>
/// Tangent constraint - a line should be tangent to a circle/arc
/// </summary>
public class TangentConstraint : TwoEntityConstraint
{
    private readonly Exp _distanceEquation;

    public TangentConstraint(LineEntity line, CircleEntity circle)
        : base(line, circle)
    {
        // Distance from line to circle center should equal radius
        var lineDir = new ExpVector2d(line.End.X - line.Start.X, line.End.Y - line.Start.Y);
        var toCenter = new ExpVector2d(circle.Center.X - line.Start.X, circle.Center.Y - line.Start.Y);
        
        // Project center onto line and get perpendicular distance
        var dot = ExpVector2d.Dot(toCenter, lineDir);
        var lineLenSq = ExpVector2d.Dot(lineDir, lineDir);
        var projection = dot / Exp.Sqrt(lineLenSq);
        
        // Closest point on line to center
        var closestX = line.Start.X + (line.End.X - line.Start.X) * projection;
        var closestY = line.Start.Y + (line.End.Y - line.Start.Y) * projection;
        
        // Distance from closest point to center minus radius should be zero
        var distSq = (circle.Center.X - closestX) * (circle.Center.X - closestX) +
                     (circle.Center.Y - closestY) * (circle.Center.Y - closestY);
        _distanceEquation = distSq - circle.Radius * circle.Radius;
    }

    public TangentConstraint(LineEntity line, ArcEntity arc)
        : base(line, arc)
    {
        // Similar to circle tangent
        var lineDir = new ExpVector2d(line.End.X - line.Start.X, line.End.Y - line.Start.Y);
        var toCenter = new ExpVector2d(arc.Center.X - line.Start.X, arc.Center.Y - line.Start.Y);
        
        var dot = ExpVector2d.Dot(toCenter, lineDir);
        var lineLenSq = ExpVector2d.Dot(lineDir, lineDir);
        var projection = dot / Exp.Sqrt(lineLenSq);
        
        var closestX = line.Start.X + (line.End.X - line.Start.X) * projection;
        var closestY = line.Start.Y + (line.End.Y - line.Start.Y) * projection;
        
        var distSq = (arc.Center.X - closestX) * (arc.Center.X - closestX) +
                     (arc.Center.Y - closestY) * (arc.Center.Y - closestY);
        _distanceEquation = distSq - arc.Radius * arc.Radius;
    }

    public override string DisplayName => "Tangent";

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
        system.RemoveEquation(_distanceEquation);
    }
}

/// <summary>
/// Equal length constraint - two lines should have the same length
/// </summary>
public class EqualLengthConstraint : TwoEntityConstraint
{
    private readonly Exp _lengthDiff;

    public EqualLengthConstraint(LineEntity line1, LineEntity line2)
        : base(line1, line2)
    {
        // Calculate length difference
        var dx1 = line1.End.X - line1.Start.X;
        var dy1 = line1.End.Y - line1.Start.Y;
        var dx2 = line2.End.X - line2.Start.X;
        var dy2 = line2.End.Y - line2.Start.Y;
        
        var len1Sq = dx1 * dx1 + dy1 * dy1;
        var len2Sq = dx2 * dx2 + dy2 * dy2;
        
        _lengthDiff = len1Sq - len2Sq;
    }

    public override string DisplayName => "Equal Length";

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
        system.AddEquation(_lengthDiff);
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
        system.RemoveEquation(_lengthDiff);
    }
}

/// <summary>
/// Fixation constraint - fix a point's position
/// </summary>
public class FixationConstraint : SingleEntityConstraint
{
    private readonly double _targetX;
    private readonly double _targetY;

    public FixationConstraint(PointEntity point, Vec2 position)
        : base(point)
    {
        _targetX = position.X;
        _targetY = position.Y;
    }

    public override string DisplayName => "Fixation";

    public override int DegreesOfFreedom => 2;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity is PointEntity point)
        {
            point.SetupEquations(system);
            // Fix X coordinate
            var dx = new Param("fix_x_" + Id.ToString("N").Substring(0, 4), _targetX);
            system.AddEquation(new EqExp(point.X, dx));
            
            // Fix Y coordinate
            var dy = new Param("fix_y_" + Id.ToString("N").Substring(0, 4), _targetY);
            system.AddEquation(new EqExp(point.Y, dy));
        }
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity is PointEntity point)
        {
            point.RemoveEquations(system);
        }
    }
}
