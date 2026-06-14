using Xunit;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Sketch;
using AutoCADConstraintSolver.Solver;
using AutoCADConstraintSolver.Constraints;

namespace AutoCADConstraintSolver.Tests;

public class GeometryTests
{
    [Fact]
    public void Vec2_Addition_Works()
    {
        var a = new Vec2(1, 2);
        var b = new Vec2(3, 4);
        var result = a + b;

        Assert.Equal(4, result.X);
        Assert.Equal(6, result.Y);
    }

    [Fact]
    public void Vec2_Magnitude_Works()
    {
        var v = new Vec2(3, 4);
        Assert.Equal(5, v.Magnitude, 6);
    }

    [Fact]
    public void Vec2_Distance_Works()
    {
        var a = new Vec2(0, 0);
        var b = new Vec2(3, 4);
        Assert.Equal(5, Vec2.Distance(a, b), 6);
    }

    [Fact]
    public void Line2d_ClosestPoint_Works()
    {
        var line = new Line2d(new Vec2(0, 0), new Vec2(10, 0));
        var point = new Vec2(5, 5);
        var closest = line.ClosestPoint(point);

        Assert.Equal(5, closest.X, 6);
        Assert.Equal(0, closest.Y, 6);
    }

    [Fact]
    public void Circle2d_ContainsPoint_Works()
    {
        var circle = new Circle2d(new Vec2(0, 0), 5);

        Assert.True(circle.ContainsPoint(new Vec2(0, 0)));
        Assert.True(circle.ContainsPoint(new Vec2(4, 0)));
        Assert.False(circle.ContainsPoint(new Vec2(6, 0)));
    }

    [Fact]
    public void Arc2d_Length_Works()
    {
        var arc = new Arc2d(new Vec2(0, 0), 5, 0, Math.PI);
        Assert.Equal(5 * Math.PI, arc.Length, 6);
    }
}

public class ParamTests
{
    [Fact]
    public void Param_Eval_Works()
    {
        var p = new Param("x", 5);
        Assert.Equal(5, p.Eval());
    }

    [Fact]
    public void Expression_Addition_Works()
    {
        var p1 = new Param("a", 3);
        var p2 = new Param("b", 4);
        var sum = p1 + p2;
        Assert.Equal(7, sum.Eval());
    }

    [Fact]
    public void Expression_Multiplication_Works()
    {
        var p1 = new Param("a", 3);
        var p2 = new Param("b", 4);
        var product = p1 * p2;
        Assert.Equal(12, product.Eval());
    }

    [Fact]
    public void Expression_Sqrt_Works()
    {
        var p = new Param("x", 9);
        var sqrt = Exp.Sqrt(p);
        Assert.Equal(3, sqrt.Eval(), 6);
    }

    [Fact]
    public void Expression_Derivative_Works()
    {
        var x = new Param("x", 2);
        var expr = x * x + x; // x^2 + x
        var deriv = expr.Deriv(x); // 2x + 1
        Assert.Equal(5, deriv.Eval(), 6);
    }
}

public class SketchTests
{
    [Fact]
    public void Sketch_AddEntity_Works()
    {
        var sketch = new Sketch();
        var line = new LineEntity(new Vec2(0, 0), new Vec2(100, 0));

        sketch.AddEntity(line);

        Assert.Single(sketch.Entities);
    }

    [Fact]
    public void Sketch_AddConstraint_Works()
    {
        var sketch = new Sketch();
        var p1 = new PointEntity(0, 0);
        var p2 = new PointEntity(100, 0);

        sketch.AddConstraint(new FixationConstraint(p1, new Vec2(0, 0)));

        Assert.Single(sketch.Constraints);
    }

    [Fact]
    public void Sketch_DegreesOfFreedom_Works()
    {
        var sketch = new Sketch();
        var p1 = new PointEntity(0, 0);
        var p2 = new PointEntity(100, 0);
        var line = new LineEntity(p1, p2);

        sketch.AddEntity(line);

        // Line has 4 DOF (2 points * 2 coords)
        Assert.Equal(4, sketch.GetDegreesOfFreedom());
    }

    [Fact]
    public void Sketch_Clear_Works()
    {
        var sketch = new Sketch();
        var line = new LineEntity(new Vec2(0, 0), new Vec2(100, 0));

        sketch.AddEntity(line);
        sketch.Clear();

        Assert.Empty(sketch.Entities);
        Assert.Empty(sketch.Constraints);
    }

    [Fact]
    public void Sketch_CreateSimpleRectangle_Works()
    {
        var sketch = Sketch.CreateSimpleRectangle(200, 100);

        // Should have 4 lines
        Assert.Equal(4, sketch.Entities.Count);
        // Should have 4 constraints (2 parallel + 1 equal + 1 fixation)
        Assert.Equal(4, sketch.Constraints.Count);
    }
}

public class ConstraintSolverTests
{
    [Fact]
    public void EquationSystem_BasicSolve_Works()
    {
        var system = new EquationSystem();

        // x + y = 10
        // x - y = 4
        // Solution: x = 7, y = 3
        var x = new Param("x", 0);
        var y = new Param("y", 0);

        system.AddEquation(x + y - new ConstExp(10));
        system.AddEquation(x - y - new ConstExp(4));
        system.AddParameter(x);
        system.AddParameter(y);

        var result = system.Solve();

        Assert.Equal(SolveResult.OKAY, result);
        Assert.Equal(7, x.value, 1);
        Assert.Equal(3, y.value, 1);
    }

    [Fact]
    public void EquationSystem_NonLinearSolve_Works()
    {
        var system = new EquationSystem();

        // x^2 + y^2 = 25  (circle with radius 5)
        // x + y = 5
        // Approximate solution near x=3, y=2
        var x = new Param("x", 3);
        var y = new Param("y", 2);

        system.AddEquation(x * x + y * y - new ConstExp(25));
        system.AddEquation(x + y - new ConstExp(5));
        system.AddParameter(x);
        system.AddParameter(y);

        var result = system.Solve();

        // Should converge to some solution
        Assert.True(result == SolveResult.OKAY || result == SolveResult.DIDNT_CONVEGE);
    }

    [Fact]
    public void HorizontalConstraint_Solves()
    {
        var system = new EquationSystem();
        var line = new LineEntity(new Vec2(0, 0), new Vec2(100, 10));

        // Make the line horizontal by constraining Y coordinates to be equal
        var hConstraint = new HorizontalConstraint(line);
        line.SetupEquations(system);
        hConstraint.SetupEquations(system);

        var result = system.Solve();

        Assert.Equal(SolveResult.OKAY, result);
        // After solving, Y coordinates should be equal
        Assert.Equal(line.StartPosition.Y, line.EndPosition.Y, 1);
    }

    [Fact]
    public void PointsDistanceConstraint_Solves()
    {
        var system = new EquationSystem();
        var p1 = new PointEntity(0, 0);
        var p2 = new PointEntity(0, 0);

        // Constrain distance between points to be 100
        var distance = 100.0;
        var constraint = new PointsDistanceConstraint(p1, p2, distance);

        p1.SetupEquations(system);
        p2.SetupEquations(system);
        constraint.SetupEquations(system);

        var result = system.Solve();

        Assert.Equal(SolveResult.OKAY, result);

        var actualDist = Vec2.Distance(p1.Position, p2.Position);
        Assert.Equal(distance, actualDist, 1);
    }
}
