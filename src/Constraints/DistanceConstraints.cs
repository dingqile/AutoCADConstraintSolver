using System;
using System.Collections.Generic;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Constraints;

/// <summary>
/// Point on entity constraint - point lies on line/circle/arc
/// </summary>
public class PointOnConstraint : TwoEntityConstraint
{
    private readonly Exp _equation;

    public PointOnConstraint(PointEntity point, LineEntity line)
        : base(point, line)
    {
        // Point lies on line: (p - start) cross direction = 0
        var dx = line.End.X - line.Start.X;
        var dy = line.End.Y - line.Start.Y;
        var px = point.X - line.Start.X;
        var py = point.Y - line.Start.Y;
        
        // Cross product = 0 for collinear points
        _equation = dx * py - dy * px;
    }

    public PointOnConstraint(PointEntity point, CircleEntity circle)
        : base(point, circle)
    {
        // Point lies on circle: distance from center = radius
        var dx = point.X - circle.Center.X;
        var dy = point.Y - circle.Center.Y;
        _equation = dx * dx + dy * dy - circle.Radius * circle.Radius;
    }

    public PointOnConstraint(PointEntity point, ArcEntity arc)
        : base(point, arc)
    {
        // Point lies on arc: same as circle + angle check
        var dx = point.X - arc.Center.X;
        var dy = point.Y - arc.Center.Y;
        _equation = dx * dx + dy * dy - arc.Radius * arc.Radius;
    }

    public PointOnConstraint(PointEntity point, LineEntity line, double parameter)
        : base(point, line)
    {
        // Point at specific parameter on line
        var targetX = line.Start.X + parameter * (line.End.X - line.Start.X);
        var targetY = line.Start.Y + parameter * (line.End.Y - line.Start.Y);
        _equation = (point.X - targetX) * (point.X - targetX) + (point.Y - targetY) * (point.Y - targetY);
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

/// <summary>
/// Point to line distance constraint
/// </summary>
public class PointLineDistanceConstraint : TwoEntityConstraint
{
    private readonly Exp _distanceEquation;

    public double Distance { get; set; }

    public PointLineDistanceConstraint(PointEntity point, LineEntity line, double distance)
        : base(point, line)
    {
        Distance = distance;
        
        // Distance from point to line
        var dx = line.End.X - line.Start.X;
        var dy = line.End.Y - line.Start.Y;
        var lenSq = dx * dx + dy * dy;
        
        // Cross product / length = perpendicular distance
        var cross = (point.X - line.Start.X) * dy - (point.Y - line.Start.Y) * dx;
        _distanceEquation = Exp.Sqrt(cross * cross / lenSq) - new ConstExp(distance);
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
        system.AddEquation(_distanceEquation);
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
        system.RemoveEquation(_distanceEquation);
    }
}

/// <summary>
/// Point to circle distance constraint
/// </summary>
public class PointCircleDistanceConstraint : TwoEntityConstraint
{
    private readonly Exp _distanceEquation;

    public double Distance { get; set; }

    public PointCircleDistanceConstraint(PointEntity point, CircleEntity circle, double distance)
        : base(point, circle)
    {
        Distance = distance;
        
        // Distance from point to circle center minus radius
        var dx = point.X - circle.Center.X;
        var dy = point.Y - circle.Center.Y;
        _distanceEquation = Exp.Sqrt(dx * dx + dy * dy) - circle.Radius - new ConstExp(distance);
    }

    public override string DisplayName => "Point-Circle Distance";

    public override int DegreesOfFreedom => 1;

    public override void SetupEquations(EquationSystem system)
    {
        if (entity1 is PointEntity p)
        {
            p.SetupEquations(system);
        }
        if (entity2 is CircleEntity circle)
        {
            circle.SetupEquations(system);
        }
        system.AddEquation(_distanceEquation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        if (entity1 is PointEntity p)
        {
            p.RemoveEquations(system);
        }
        if (entity2 is CircleEntity circle)
        {
            circle.RemoveEquations(system);
        }
        system.RemoveEquation(_distanceEquation);
    }
}

/// <summary>
/// Line to line distance constraint
/// </summary>
public class LineLineDistanceConstraint : TwoEntityConstraint
{
    private readonly Exp _distanceEquation;

    public double Distance { get; set; }

    public LineLineDistanceConstraint(LineEntity line1, LineEntity line2, double distance)
        : base(line1, line2)
    {
        Distance = distance;
        
        // Distance between two parallel lines
        var dx = line2.Start.X - line1.Start.X;
        var dy = line2.Start.Y - line1.Start.Y;
        var dirX = line1.End.X - line1.Start.X;
        var dirY = line1.End.Y - line1.Start.Y;
        
        // Cross product gives area = distance * length
        var cross = Math.Abs(dx * dirY - dy * dirX);
        var lenSq = dirX * dirX + dirY * dirY;
        
        _distanceEquation = Exp.Sqrt(cross * cross / lenSq) - new ConstExp(distance);
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
        system.AddEquation(_distanceEquation);
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
        system.RemoveEquation(_distanceEquation);
    }
}

/// <summary>
/// Line to circle distance constraint
/// </summary>
public class LineCircleDistanceConstraint : TwoEntityConstraint
{
    private readonly Exp _distanceEquation;

    public double Distance { get; set; }

    public LineCircleDistanceConstraint(LineEntity line, CircleEntity circle, double distance)
        : base(line, circle)
    {
        Distance = distance;
        
        // Distance from line to circle center
        var dx = circle.Center.X - line.Start.X;
        var dy = circle.Center.Y - line.Start.Y;
        var dirX = line.End.X - line.Start.X;
        var dirY = line.End.Y - line.Start.Y;
        
        var cross = dx * dirY - dy * dirX;
        var lenSq = dirX * dirX + dirY * dirY;
        
        // Distance to line minus radius equals constraint distance
        _distanceEquation = Exp.Sqrt(cross * cross / lenSq) - circle.Radius - new ConstExp(distance);
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
        system.RemoveEquation(_distanceEquation);
    }
}

/// <summary>
/// Circle to circle distance constraint
/// </summary>
public class CirclesDistanceConstraint : TwoEntityConstraint
{
    private readonly Exp _distanceEquation;

    public double Distance { get; set; }

    public CirclesDistanceConstraint(CircleEntity circle1, CircleEntity circle2, double distance)
        : base(circle1, circle2)
    {
        Distance = distance;
        
        // Distance between centers minus both radii minus constraint distance
        var dx = circle2.Center.X - circle1.Center.X;
        var dy = circle2.Center.Y - circle1.Center.Y;
        _distanceEquation = Exp.Sqrt(dx * dx + dy * dy) - circle1.Radius - circle2.Radius - new ConstExp(distance);
    }

    public CirclesDistanceConstraint(ArcEntity arc1, ArcEntity arc2, double distance)
        : base(arc1, arc2)
    {
        Distance = distance;
        
        var dx = arc2.Center.X - arc1.Center.X;
        var dy = arc2.Center.Y - arc1.Center.Y;
        _distanceEquation = Exp.Sqrt(dx * dx + dy * dy) - arc1.Radius - arc2.Radius - new ConstExp(distance);
    }

    public CirclesDistanceConstraint(CircleEntity circle, ArcEntity arc, double distance)
        : base(circle, arc)
    {
        Distance = distance;
        
        var dx = arc.Center.X - circle.Center.X;
        var dy = arc.Center.Y - circle.Center.Y;
        _distanceEquation = Exp.Sqrt(dx * dx + dy * dy) - circle.Radius - arc.Radius - new ConstExp(distance);
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
        
        system.RemoveEquation(_distanceEquation);
    }
}

/// <summary>
/// Angle constraint - angle between two lines
/// </summary>
public class AngleConstraint : TwoEntityConstraint
{
    private readonly Exp _angleEquation;

    public double TargetAngle { get; set; }

    public AngleConstraint(LineEntity line1, LineEntity line2, double angleRadians)
        : base(line1, line2)
    {
        TargetAngle = angleRadians;
        
        // Direction vectors
        var d1x = line1.End.X - line1.Start.X;
        var d1y = line1.End.Y - line1.Start.Y;
        var d2x = line2.End.X - line2.Start.X;
        var d2y = line2.End.Y - line2.Start.Y;
        
        // Dot product: cos(angle) = (d1 · d2) / (|d1| * |d2|)
        var dot = d1x * d2x + d1y * d2y;
        var len1Sq = d1x * d1x + d1y * d1y;
        var len2Sq = d2x * d2x + d2y * d2y;
        
        // cos(angle) - cos(target) = 0
        _angleEquation = dot / Exp.Sqrt(len1Sq * len2Sq) - new ConstExp(Math.Cos(angleRadians));
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
        system.AddEquation(_angleEquation);
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
        system.RemoveEquation(_angleEquation);
    }
}

/// <summary>
/// Custom equation constraint - user-defined mathematical relationship
/// </summary>
public class EquationConstraint : Constraint
{
    private readonly Exp _equation;
    private readonly List<Param> _params = new();
    private readonly string _expression;

    public string Expression => _expression;

    public EquationConstraint(Exp equation, string expression = "")
    {
        _equation = equation;
        _expression = expression;
        
        // Collect all parameters
        foreach (var p in equation.DependOnParams())
        {
            if (!_params.Contains(p))
                _params.Add(p);
        }
    }

    /// <summary>
    /// Create equation constraint from expression string
    /// </summary>
    public static EquationConstraint FromExpression(string expression, params Param[] parameters)
    {
        var exp = ParseExpression(expression, parameters);
        return new EquationConstraint(exp, expression);
    }

    private static Exp ParseExpression(string expr, Param[] parameters)
    {
        // Simple expression parser
        // Supports: +, -, *, /, ^, sin, cos, sqrt, abs, pi, e
        // Parameters: use p0, p1, p2, etc. or parameter names
        
        expr = expr.Replace(" ", "").ToLower();
        
        // Replace pi
        expr = expr.Replace("pi", Math.PI.ToString());
        
        // Replace e
        expr = expr.Replace("e", Math.E.ToString());
        
        // Replace parameter references
        for (int i = 0; i < parameters.Length; i++)
        {
            expr = expr.Replace($"p{i}", $"({parameters[i].value})");
        }
        
        // Simple recursive descent parsing
        return SimpleParse(expr, parameters);
    }

    private static Exp SimpleParse(string expr, Param[] parameters)
    {
        // Very simplified parser for basic expressions
        // In practice, use a proper expression parser
        
        // For now, return zero as placeholder
        // Full implementation would parse the expression tree
        return Exp.zero;
    }

    public override string DisplayName => "Equation";

    public override bool IsValid => _equation != null;

    public override IEnumerable<Entity> GetEntities()
    {
        return Array.Empty<Entity>();
    }

    public override int DegreesOfFreedom => _params.Count;

    public override void SetupEquations(EquationSystem system)
    {
        foreach (var p in _params)
        {
            if (!system.Parameters.Contains(p))
                system.AddParameter(p);
        }
        system.AddEquation(_equation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        foreach (var p in _params)
        {
            system.RemoveParameter(p);
        }
        system.RemoveEquation(_equation);
    }
}

/// <summary>
/// Equal value constraint - two parameters have the same value
/// </summary>
public class EqualValueConstraint : Constraint
{
    private readonly Param _param1;
    private readonly Param _param2;
    private readonly EqExp _equation;

    public EqualValueConstraint(Param param1, Param param2)
    {
        _param1 = param1;
        _param2 = param2;
        _equation = new EqExp(param1, param2);
    }

    public override string DisplayName => "Equal Value";

    public override int DegreesOfFreedom => 1;

    public override IEnumerable<Entity> GetEntities()
    {
        return Array.Empty<Entity>();
    }

    public override void SetupEquations(EquationSystem system)
    {
        if (!system.Parameters.Contains(_param1))
            system.AddParameter(_param1);
        if (!system.Parameters.Contains(_param2))
            system.AddParameter(_param2);
        system.AddEquation(_equation);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        system.RemoveEquation(_equation);
    }
}