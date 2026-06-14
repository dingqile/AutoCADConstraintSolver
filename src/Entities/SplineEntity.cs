using System;
using System.Collections.Generic;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Entities;

/// <summary>
/// Spline entity - B-spline curve
/// </summary>
public class SplineEntity : Entity
{
    public List<PointEntity> ControlPoints { get; } = new();
    public List<double> Knots { get; } = new();
    public int Degree { get; }
    
    public override EntityType Type => EntityType.Spline;

    public SplineEntity(IEnumerable<Vec2> controlPoints, int degree = 3)
    {
        Degree = degree;
        
        foreach (var point in controlPoints)
        {
            ControlPoints.Add(new PointEntity(point.X, point.Y));
        }
        
        // Generate default uniform knots
        GenerateUniformKnots();
    }

    private void GenerateUniformKnots()
    {
        int n = ControlPoints.Count - 1;
        int k = Degree + 1;
        
        // Clamped B-spline knots
        for (int i = 0; i < k; i++)
            Knots.Add(0);
        
        for (int i = 1; i < n - Degree; i++)
            Knots.Add((double)i / (n - Degree + 1));
        
        for (int i = 0; i < k; i++)
            Knots.Add(1);
    }

    /// <summary>
    /// Evaluate the spline at parameter t
    /// </summary>
    public Vec2 Evaluate(double t)
    {
        var result = Vec2.Zero;
        int n = ControlPoints.Count - 1;
        
        for (int i = 0; i <= n; i++)
        {
            var basis = BasisFunction(i, Degree, t);
            result = result + ControlPoints[i].Position * basis;
        }
        
        return result;
    }

    /// <summary>
    /// Evaluate B-spline basis function
    /// </summary>
    private double BasisFunction(int i, int k, double t)
    {
        if (k == 0)
        {
            return (t >= Knots[i] && t < Knots[i + 1]) || 
                   (t == 1 && i == ControlPoints.Count - Degree - 1) ? 1 : 0;
        }
        
        double result = 0;
        
        double denom1 = Knots[i + k] - Knots[i];
        if (denom1 != 0)
        {
            result += (t - Knots[i]) / denom1 * BasisFunction(i, k - 1, t);
        }
        
        double denom2 = Knots[i + k + 1] - Knots[i + 1];
        if (denom2 != 0)
        {
            result += (Knots[i + k + 1] - t) / denom2 * BasisFunction(i + 1, k - 1, t);
        }
        
        return result;
    }

    /// <summary>
    /// Get the tangent at parameter t
    /// </summary>
    public Vec2 Tangent(double t)
    {
        var result = Vec2.Zero;
        int n = ControlPoints.Count - 1;
        
        // Derivative of B-spline is another B-spline
        for (int i = 0; i < n; i++)
        {
            var basis = BasisFunctionDerivative(i, Degree, t);
            var dx = ControlPoints[i + 1].Position.X - ControlPoints[i].Position.X;
            var dy = ControlPoints[i + 1].Position.Y - ControlPoints[i].Position.Y;
            result = result + new Vec2(dx, dy) * basis;
        }
        
        return result.Normalized;
    }

    private double BasisFunctionDerivative(int i, int k, double t)
    {
        if (k == 0) return 0;
        
        double result = 0;
        
        double denom1 = Knots[i + k] - Knots[i];
        if (denom1 != 0)
        {
            result += k / denom1 * BasisFunction(i, k - 1, t);
            result -= k / denom1 * BasisFunction(i + 1, k - 1, t);
        }
        
        return result;
    }

    public override BBox2d GetBoundingBox()
    {
        var bbox = new BBox2d();
        
        foreach (var point in ControlPoints)
        {
            bbox.Expand(point.Position);
        }
        
        // Sample curve for better bounds
        for (int i = 0; i <= 20; i++)
        {
            bbox.Expand(Evaluate((double)i / 20));
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
        // Find closest point on spline using Newton-Raphson
        double bestT = 0;
        double bestDist = double.MaxValue;
        
        // First, find approximate closest point by sampling
        for (int i = 0; i <= 50; i++)
        {
            double t = (double)i / 50;
            var curvePoint = Evaluate(t);
            var dist = Vec2.Distance(point, curvePoint);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestT = t;
            }
        }
        
        // Refine with Newton-Raphson
        for (int iter = 0; iter < 10; iter++)
        {
            var p = Evaluate(bestT);
            var tan = Tangent(bestT);
            var toPoint = point - p;
            
            // Project onto tangent
            var dt = Vec2.Dot(toPoint, tan);
            bestT += dt * 0.1; // Damping factor
            
            bestT = Math.Clamp(bestT, 0, 1);
            
            if (Math.Abs(dt) < 1e-6) break;
        }
        
        return Evaluate(bestT);
    }

    public override IEnumerable<Param> GetParams()
    {
        var list = new List<Param>();
        foreach (var point in ControlPoints)
        {
            list.Add(point.X);
            list.Add(point.Y);
        }
        return list;
    }

    public override void SetupEquations(EquationSystem system)
    {
        foreach (var point in ControlPoints)
        {
            point.SetupEquations(system);
        }
    }

    public override void RemoveEquations(EquationSystem system)
    {
        foreach (var point in ControlPoints)
        {
            point.RemoveEquations(system);
        }
    }

    public override Vec2 GetPosition() => Evaluate(0);

    public override void Move(Vec2 delta)
    {
        foreach (var point in ControlPoints)
        {
            point.Move(delta);
        }
    }

    public override Entity Clone()
    {
        var points = new List<Vec2>();
        foreach (var point in ControlPoints)
        {
            points.Add(point.Position);
        }
        return new SplineEntity(points, Degree);
    }

    public override string ToString() => 
        $"Spline(ControlPoints={ControlPoints.Count}, Degree={Degree})";
}

/// <summary>
/// Point on spline constraint
/// </summary>
namespace AutoCADConstraintSolver.Constraints
{
    using AutoCADConstraintSolver.Entities;
    
    public class PointOnSplineConstraint : TwoEntityConstraint
    {
        private readonly SplineEntity _spline;
        private readonly PointEntity _point;
        private readonly Param _t; // Parameter on spline

        public PointOnSplineConstraint(PointEntity point, SplineEntity spline)
            : base(point, spline)
        {
            _point = point;
            _spline = spline;
            _t = new Param("t_" + Id.ToString("N").Substring(0, 8), 0.5);
        }

        public override string DisplayName => "Point On Spline";

        public override int DegreesOfFreedom => 2; // The point position

        public override IEnumerable<Entity> GetEntities() => 
            new[] { (Entity)_point, (Entity)_spline };

        public override void SetupEquations(EquationSystem system)
        {
            _point.SetupEquations(system);
            _spline.SetupEquations(system);
            system.AddParameter(_t);
            
            // x = spline.x(t), y = spline.y(t)
            // This is simplified - in practice we'd need to handle the B-spline evaluation
        }

        public override void RemoveEquations(EquationSystem system)
        {
            _point.RemoveEquations(system);
            _spline.RemoveEquations(system);
            system.RemoveParameter(_t);
        }
    }
}
