using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Entities;

/// <summary>
/// Text entity for annotations
/// </summary>
public class TextEntity : Entity
{
    public PointEntity Position { get; }
    public string Text { get; set; }
    public double Height { get; set; }
    public double Rotation { get; set; }
    public TextAlignment Alignment { get; set; }

    public override EntityType Type => EntityType.Point; // Using Point as placeholder

    public Vec2 Position2D
    {
        get => Position.Position;
        set => Position.Position = value;
    }

    public TextEntity(Vec2 position, string text, double height = 10)
    {
        Position = new PointEntity(position.X, position.Y);
        Text = text;
        Height = height;
        Rotation = 0;
        Alignment = TextAlignment.Left;
    }

    /// <summary>
    /// Get the bounding box of the text
    /// </summary>
    public override BBox2d GetBoundingBox()
    {
        // Approximate text bounds based on character count and height
        double width = Text.Length * Height * 0.6;
        double height = Height;

        var bbox = new BBox2d(Position2D, Position2D);
        
        // Expand based on text rotation
        if (Math.Abs(Rotation) < 1e-6)
        {
            bbox.Expand(Position2D + new Vec2(width, height));
        }
        else
        {
            // For rotated text, expand using bounding box calculation
            var corners = new[]
            {
                new Vec2(0, 0),
                new Vec2(width, 0),
                new Vec2(width, height),
                new Vec2(0, height)
            };

            foreach (var corner in corners)
            {
                var rotated = corner.Rotate(Rotation);
                bbox.Expand(Position2D + rotated);
            }
        }

        return bbox;
    }

    public override double DistanceTo(Vec2 point)
    {
        return Vec2.Distance(point, Position2D);
    }

    public override Vec2 ClosestPoint(Vec2 point)
    {
        return Position2D;
    }

    public override IEnumerable<Param> GetParams()
    {
        return new[] { Position.X, Position.Y };
    }

    public override void SetupEquations(EquationSystem system)
    {
        Position.SetupEquations(system);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        Position.RemoveEquations(system);
    }

    public override Vec2 GetPosition() => Position2D;

    public override void Move(Vec2 delta)
    {
        Position.Move(delta);
    }

    public override Entity Clone()
    {
        var clone = new TextEntity(Position2D, Text, Height)
        {
            Rotation = Rotation,
            Alignment = Alignment
        };
        return clone;
    }

    public override string ToString() => $"Text(\"{Text}\" at {Position2D})";
}

/// <summary>
/// Text alignment options
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right,
    AlignTop,
    AlignMiddle,
    AlignBottom
}

/// <summary>
/// Function curve entity - parametric function y = f(x)
/// </summary>
public class FunctionEntity : Entity
{
    public PointEntity Origin { get; }
    public double XMin { get; set; }
    public double XMax { get; set; }
    public string FunctionExpression { get; set; }

    // Sample points for rendering
    private readonly List<Vec2> _samplePoints = new();
    private const int SampleCount = 100;

    public override EntityType Type => EntityType.Point; // Placeholder

    public Vec2 OriginPosition
    {
        get => Origin.Position;
        set => Origin.Position = value;
    }

    public FunctionEntity(Vec2 origin, double xMin, double xMax, string functionExpression)
    {
        Origin = new PointEntity(origin.X, origin.Y);
        XMin = xMin;
        XMax = xMax;
        FunctionExpression = functionExpression;
        
        UpdateSamplePoints();
    }

    /// <summary>
    /// Evaluate the function at x
    /// </summary>
    public double Evaluate(double x)
    {
        // Simple expression parser for basic functions
        // Supports: x, sin, cos, tan, sqrt, abs, +, -, *, /, ^
        try
        {
            return EvaluateExpression(FunctionExpression, x);
        }
        catch
        {
            return 0;
        }
    }

    private double EvaluateExpression(string expr, double x)
    {
        // Remove spaces
        expr = expr.Replace(" ", "");
        
        // Handle common functions
        expr = expr.Replace("sin", "S");
        expr = expr.Replace("cos", "C");
        expr = expr.Replace("tan", "T");
        expr = expr.Replace("sqrt", "Q");
        expr = expr.Replace("abs", "A");
        expr = expr.Replace("PI", Math.PI.ToString(CultureInfo.InvariantCulture));
        
        return ParseExpression(expr, x);
    }

    private double ParseExpression(string expr, double x)
    {
        // Simple recursive descent parser
        int pos = 0;
        return ParseAddSubtract(expr, ref pos, x);
    }

    private double ParseAddSubtract(string expr, ref int pos, double x)
    {
        var left = ParseMulDiv(expr, ref pos, x);
        
        while (pos < expr.Length)
        {
            char op = expr[pos];
            if (op != '+' && op != '-') break;
            pos++;
            var right = ParseMulDiv(expr, ref pos, x);
            left = op == '+' ? left + right : left - right;
        }
        
        return left;
    }

    private double ParseMulDiv(string expr, ref int pos, double x)
    {
        var left = ParsePower(expr, ref pos, x);
        
        while (pos < expr.Length)
        {
            char op = expr[pos];
            if (op != '*' && op != '/') break;
            pos++;
            var right = ParsePower(expr, ref pos, x);
            left = op == '*' ? left * right : left / right;
        }
        
        return left;
    }

    private double ParsePower(string expr, ref int pos, double x)
    {
        var left = ParseUnary(expr, ref pos, x);
        
        if (pos < expr.Length && expr[pos] == '^')
        {
            pos++;
            var right = ParseUnary(expr, ref pos, x);
            left = Math.Pow(left, right);
        }
        
        return left;
    }

    private double ParseUnary(string expr, ref int pos, double x)
    {
        if (pos < expr.Length && expr[pos] == '-')
        {
            pos++;
            return -ParsePrimary(expr, ref pos, x);
        }
        return ParsePrimary(expr, ref pos, x);
    }

    private double ParsePrimary(string expr, ref int pos, double x)
    {
        if (pos >= expr.Length) return 0;
        
        char c = expr[pos];
        
        // Handle functions
        if (c == 'S') // sin
        {
            pos++;
            return Math.Sin(ParsePrimary(expr, ref pos, x));
        }
        if (c == 'C') // cos
        {
            pos++;
            return Math.Cos(ParsePrimary(expr, ref pos, x));
        }
        if (c == 'T') // tan
        {
            pos++;
            return Math.Tan(ParsePrimary(expr, ref pos, x));
        }
        if (c == 'Q') // sqrt
        {
            pos++;
            return Math.Sqrt(ParsePrimary(expr, ref pos, x));
        }
        if (c == 'A') // abs
        {
            pos++;
            return Math.Abs(ParsePrimary(expr, ref pos, x));
        }
        
        // Handle parentheses
        if (c == '(')
        {
            pos++;
            var result = ParseAddSubtract(expr, ref pos, x);
            if (pos < expr.Length && expr[pos] == ')')
                pos++;
            return result;
        }
        
        // Handle x variable
        if (c == 'x' || c == 'X')
        {
            pos++;
            return x;
        }
        
        // Handle numbers
        var sb = new StringBuilder();
        while (pos < expr.Length && (char.IsDigit(expr[pos]) || expr[pos] == '.'))
        {
            sb.Append(expr[pos++]);
        }
        
        if (double.TryParse(sb.ToString(), out double num))
            return num;
        
        return 0;
    }

    private void UpdateSamplePoints()
    {
        _samplePoints.Clear();
        double step = (XMax - XMin) / SampleCount;
        
        for (int i = 0; i <= SampleCount; i++)
        {
            double x = XMin + i * step;
            double y = Evaluate(x);
            _samplePoints.Add(new Vec2(x, y));
        }
    }

    /// <summary>
    /// Get sample points for rendering
    /// </summary>
    public IReadOnlyList<Vec2> GetSamplePoints()
    {
        return _samplePoints.AsReadOnly();
    }

    public override BBox2d GetBoundingBox()
    {
        var bbox = new BBox2d();
        
        foreach (var p in _samplePoints)
        {
            bbox.Expand(OriginPosition + p);
        }
        
        return bbox;
    }

    public override double DistanceTo(Vec2 point)
    {
        double minDist = double.MaxValue;
        
        foreach (var p in _samplePoints)
        {
            var worldPoint = OriginPosition + p;
            var dist = Vec2.Distance(point, worldPoint);
            if (dist < minDist)
                minDist = dist;
        }
        
        return minDist;
    }

    public override Vec2 ClosestPoint(Vec2 point)
    {
        Vec2 closest = Vec2.Zero;
        double minDist = double.MaxValue;
        
        foreach (var p in _samplePoints)
        {
            var worldPoint = OriginPosition + p;
            var dist = Vec2.Distance(point, worldPoint);
            if (dist < minDist)
            {
                minDist = dist;
                closest = worldPoint;
            }
        }
        
        return closest;
    }

    public override IEnumerable<Param> GetParams()
    {
        return new[] { Origin.X, Origin.Y };
    }

    public override void SetupEquations(EquationSystem system)
    {
        Origin.SetupEquations(system);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        Origin.RemoveEquations(system);
    }

    public override Vec2 GetPosition() => OriginPosition;

    public override void Move(Vec2 delta)
    {
        Origin.Move(delta);
    }

    public override Entity Clone()
    {
        return new FunctionEntity(OriginPosition, XMin, XMax, FunctionExpression);
    }

    public override string ToString() => $"Function(y = {FunctionExpression})";
}

/// <summary>
/// Offset entity - offset of another entity
/// </summary>
public class OffsetEntity : Entity
{
    public Entity SourceEntity { get; }
    public double OffsetDistance { get; set; }

    public override EntityType Type => EntityType.Point; // Placeholder

    public OffsetEntity(Entity sourceEntity, double offsetDistance)
    {
        SourceEntity = sourceEntity;
        OffsetDistance = offsetDistance;
    }

    public Vec2 GetPointAt(double t)
    {
        // Get point on source and offset by normal
        var point = SourceEntity switch
        {
            LineEntity line => line.GetPointAt(t),
            ArcEntity arc => arc.GetPointAt(t),
            CircleEntity circle => circle.ClosestPointTo(point),
            _ => Vec2.Zero
        };

        // Get normal at that point
        var normal = GetNormalAt(t);
        return point + normal * OffsetDistance;
    }

    public Vec2 GetNormalAt(double t)
    {
        // Get appropriate normal based on entity type
        return SourceEntity switch
        {
            LineEntity line => line.Normal,
            ArcEntity arc => GetArcNormal(arc, t),
            CircleEntity circle => (GetCircleClosestPoint(circle, Vec2.Zero) - circle.CenterPosition).Normalized,
            _ => Vec2.UnitY
        };
    }

    private Vec2 GetArcNormal(ArcEntity arc, double t)
    {
        var point = arc.GetPointAt(t);
        return (point - arc.CenterPosition).Normalized;
    }

    private Vec2 GetCircleClosestPoint(CircleEntity circle, Vec2 point)
    {
        return circle.ClosestPointTo(point);
    }

    public override BBox2d GetBoundingBox()
    {
        var sourceBBox = SourceEntity.GetBoundingBox();
        var offset = Math.Abs(OffsetDistance);
        
        return new BBox2d(
            sourceBBox.Min - new Vec2(offset, offset),
            sourceBBox.Max + new Vec2(offset, offset));
    }

    public override double DistanceTo(Vec2 point)
    {
        return SourceEntity.DistanceTo(point) - Math.Abs(OffsetDistance);
    }

    public override Vec2 ClosestPoint(Vec2 point)
    {
        var closest = SourceEntity.ClosestPoint(point);
        var normal = (closest - GetNormalAt(0)).Normalized;
        return closest + normal * OffsetDistance;
    }

    public override IEnumerable<Param> GetParams()
    {
        return SourceEntity.GetParams();
    }

    public override void SetupEquations(EquationSystem system)
    {
        SourceEntity.SetupEquations(system);
    }

    public override void RemoveEquations(EquationSystem system)
    {
        SourceEntity.RemoveEquations(system);
    }

    public override Vec2 GetPosition() => SourceEntity.GetPosition();

    public override void Move(Vec2 delta)
    {
        if (SourceEntity is ICanMove moveable)
        {
            moveable.Move(delta);
        }
    }

    public override Entity Clone()
    {
        return new OffsetEntity(SourceEntity.Clone(), OffsetDistance);
    }

    public override string ToString() => $"Offset({SourceEntity}, {OffsetDistance})";
}

/// <summary>
/// Interface for movable entities
/// </summary>
public interface ICanMove
{
    void Move(Vec2 delta);
}