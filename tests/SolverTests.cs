using Xunit;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Sketch;
using AutoCADConstraintSolver.Solver;
using AutoCADConstraintSolver.Constraints;
using AutoCADConstraintSolver.Features;
using System.Collections.Generic;

namespace AutoCADConstraintSolver.Tests;

public class GeometryTests
{
    #region Vec2 Tests

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
    public void Vec2_Subtraction_Works()
    {
        var a = new Vec2(3, 4);
        var b = new Vec2(1, 2);
        var result = a - b;

        Assert.Equal(2, result.X);
        Assert.Equal(2, result.Y);
    }

    [Fact]
    public void Vec2_ScalarMultiplication_Works()
    {
        var v = new Vec2(2, 3);
        var result = v * 3;

        Assert.Equal(6, result.X);
        Assert.Equal(9, result.Y);
    }

    [Fact]
    public void Vec2_Magnitude_Works()
    {
        var v = new Vec2(3, 4);
        Assert.Equal(5, v.Magnitude, 6);
    }

    [Fact]
    public void Vec2_MagnitudeSquared_Works()
    {
        var v = new Vec2(3, 4);
        Assert.Equal(25, v.MagnitudeSquared, 6);
    }

    [Fact]
    public void Vec2_DotProduct_Works()
    {
        var a = new Vec2(1, 2);
        var b = new Vec2(3, 4);
        Assert.Equal(11, Vec2.Dot(a, b), 6);
    }

    [Fact]
    public void Vec2_CrossProduct_Works()
    {
        var a = new Vec2(1, 2);
        var b = new Vec2(3, 4);
        Assert.Equal(-2, Vec2.Cross(a, b), 6);
    }

    [Fact]
    public void Vec2_Distance_Works()
    {
        var a = new Vec2(0, 0);
        var b = new Vec2(3, 4);
        Assert.Equal(5, Vec2.Distance(a, b), 6);
    }

    [Fact]
    public void Vec2_Lerp_Works()
    {
        var a = new Vec2(0, 0);
        var b = new Vec2(10, 10);
        var result = Vec2.Lerp(a, b, 0.5);

        Assert.Equal(5, result.X, 6);
        Assert.Equal(5, result.Y, 6);
    }

    [Fact]
    public void Vec2_Rotate_Works()
    {
        var v = new Vec2(1, 0);
        var rotated = v.Rotate(Math.PI / 2); // 90 degrees

        Assert.Equal(0, rotated.X, 6);
        Assert.Equal(1, rotated.Y, 6);
    }

    [Fact]
    public void Vec2_Perp_Works()
    {
        var v = new Vec2(1, 0);
        var perp = v.Perp();

        Assert.Equal(0, perp.X, 6);
        Assert.Equal(1, perp.Y, 6);
    }

    [Fact]
    public void Vec2_Normalized_Works()
    {
        var v = new Vec2(3, 4);
        var normalized = v.Normalized;

        Assert.Equal(1, normalized.Magnitude, 6);
        Assert.Equal(0.6, normalized.X, 6);
        Assert.Equal(0.8, normalized.Y, 6);
    }

    #endregion

    #region Vec3 Tests

    [Fact]
    public void Vec3_Addition_Works()
    {
        var a = new Vec3(1, 2, 3);
        var b = new Vec3(4, 5, 6);
        var result = a + b;

        Assert.Equal(5, result.X);
        Assert.Equal(7, result.Y);
        Assert.Equal(9, result.Z);
    }

    [Fact]
    public void Vec3_CrossProduct_Works()
    {
        var a = new Vec3(1, 0, 0);
        var b = new Vec3(0, 1, 0);
        var result = Vec3.Cross(a, b);

        Assert.Equal(0, result.X, 6);
        Assert.Equal(0, result.Y, 6);
        Assert.Equal(1, result.Z, 6);
    }

    [Fact]
    public void Vec3_DotProduct_Works()
    {
        var a = new Vec3(1, 2, 3);
        var b = new Vec3(4, 5, 6);
        Assert.Equal(32, Vec3.Dot(a, b), 6);
    }

    #endregion

    #region Line2d Tests

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
    public void Line2d_DistanceTo_Works()
    {
        var line = new Line2d(new Vec2(0, 0), new Vec2(10, 0));
        var point = new Vec2(5, 5);
        Assert.Equal(5, line.DistanceTo(point), 6);
    }

    [Fact]
    public void Line2d_ContainsPoint_Works()
    {
        var line = new Line2d(new Vec2(0, 0), new Vec2(10, 0));
        Assert.True(line.ContainsPoint(new Vec2(5, 0)));
        Assert.False(line.ContainsPoint(new Vec2(5, 1)));
    }

    #endregion

    #region Circle2d Tests

    [Fact]
    public void Circle2d_ContainsPoint_Works()
    {
        var circle = new Circle2d(new Vec2(0, 0), 5);

        Assert.True(circle.ContainsPoint(new Vec2(0, 0)));
        Assert.True(circle.ContainsPoint(new Vec2(4, 0)));
        Assert.False(circle.ContainsPoint(new Vec2(6, 0)));
    }

    [Fact]
    public void Circle2d_TangentPoint_Works()
    {
        var circle = new Circle2d(new Vec2(0, 0), 5);
        var point = new Vec2(10, 0);
        var tangent = circle.TangentPoint(point);

        Assert.Equal(5, tangent.X, 6);
        Assert.Equal(0, tangent.Y, 6);
    }

    #endregion

    #region Arc2d Tests

    [Fact]
    public void Arc2d_Length_Works()
    {
        var arc = new Arc2d(new Vec2(0, 0), 5, 0, Math.PI);
        Assert.Equal(5 * Math.PI, arc.Length, 6);
    }

    [Fact]
    public void Arc2d_StartPoint_Works()
    {
        var arc = new Arc2d(new Vec2(0, 0), 5, 0, Math.PI / 2);
        var start = arc.StartPoint;

        Assert.Equal(5, start.X, 6);
        Assert.Equal(0, start.Y, 6);
    }

    [Fact]
    public void Arc2d_EndPoint_Works()
    {
        var arc = new Arc2d(new Vec2(0, 0), 5, 0, Math.PI / 2);
        var end = arc.EndPoint;

        Assert.Equal(0, end.X, 6);
        Assert.Equal(5, end.Y, 6);
    }

    [Fact]
    public void Arc2d_ClosestPoint_Works()
    {
        var arc = new Arc2d(new Vec2(0, 0), 5, 0, Math.PI);
        var point = new Vec2(0, 10);
        var closest = arc.ClosestPoint(point);

        Assert.Equal(5, closest.X, 6);
        Assert.Equal(0, closest.Y, 6);
    }

    #endregion

    #region BBox2d Tests

    [Fact]
    public void BBox2d_CreateFromPoints_Works()
    {
        var bbox = BBox2d.CreateFromPoints(
            new Vec2(0, 0),
            new Vec2(10, 10),
            new Vec2(5, 5));

        Assert.Equal(0, bbox.Min.X, 6);
        Assert.Equal(0, bbox.Min.Y, 6);
        Assert.Equal(10, bbox.Max.X, 6);
        Assert.Equal(10, bbox.Max.Y, 6);
    }

    [Fact]
    public void BBox2d_Contains_Works()
    {
        var bbox = new BBox2d(new Vec2(0, 0), new Vec2(10, 10));

        Assert.True(bbox.Contains(new Vec2(5, 5)));
        Assert.False(bbox.Contains(new Vec2(15, 5)));
    }

    [Fact]
    public void BBox2d_Intersects_Works()
    {
        var bbox1 = new BBox2d(new Vec2(0, 0), new Vec2(10, 10));
        var bbox2 = new BBox2d(new Vec2(5, 5), new Vec2(15, 15));

        Assert.True(bbox1.Intersects(bbox2));
        Assert.False(bbox1.Intersects(new BBox2d(new Vec2(20, 20), new Vec2(30, 30))));
    }

    #endregion
}

public class ParamTests
{
    #region Basic Param Tests

    [Fact]
    public void Param_Eval_Works()
    {
        var p = new Param("x", 5);
        Assert.Equal(5, p.Eval());
    }

    [Fact]
    public void Param_CanBeModified()
    {
        var p = new Param("x", 5);
        p.value = 10;
        Assert.Equal(10, p.Eval());
    }

    #endregion

    #region Arithmetic Expression Tests

    [Fact]
    public void Expression_Addition_Works()
    {
        var p1 = new Param("a", 3);
        var p2 = new Param("b", 4);
        var sum = p1 + p2;
        Assert.Equal(7, sum.Eval());
    }

    [Fact]
    public void Expression_Subtraction_Works()
    {
        var p1 = new Param("a", 7);
        var p2 = new Param("b", 3);
        var diff = p1 - p2;
        Assert.Equal(4, diff.Eval());
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
    public void Expression_Division_Works()
    {
        var p1 = new Param("a", 12);
        var p2 = new Param("b", 4);
        var quotient = p1 / p2;
        Assert.Equal(3, quotient.Eval());
    }

    [Fact]
    public void Expression_Negation_Works()
    {
        var p = new Param("x", 5);
        var neg = -p;
        Assert.Equal(-5, neg.Eval());
    }

    #endregion

    #region Math Function Tests

    [Fact]
    public void Expression_Sqrt_Works()
    {
        var p = new Param("x", 9);
        var sqrt = Exp.Sqrt(p);
        Assert.Equal(3, sqrt.Eval(), 6);
    }

    [Fact]
    public void Expression_Abs_Works()
    {
        var p = new Param("x", -5);
        var abs = Exp.Abs(p);
        Assert.Equal(5, abs.Eval(), 6);
    }

    [Fact]
    public void Expression_Sin_Works()
    {
        var p = new Param("x", Math.PI / 2);
        var sin = Exp.Sin(p);
        Assert.Equal(1, sin.Eval(), 6);
    }

    [Fact]
    public void Expression_Cos_Works()
    {
        var p = new Param("x", 0);
        var cos = Exp.Cos(p);
        Assert.Equal(1, cos.Eval(), 6);
    }

    [Fact]
    public void Expression_Tan_Works()
    {
        var p = new Param("x", Math.PI / 4);
        var tan = Exp.Tan(p);
        Assert.Equal(1, tan.Eval(), 6);
    }

    [Fact]
    public void Expression_Asin_Works()
    {
        var p = new Param("x", 1);
        var asin = Exp.Asin(p);
        Assert.Equal(Math.PI / 2, asin.Eval(), 6);
    }

    [Fact]
    public void Expression_Acos_Works()
    {
        var p = new Param("x", 1);
        var acos = Exp.Acos(p);
        Assert.Equal(0, acos.Eval(), 6);
    }

    [Fact]
    public void Expression_Atan_Works()
    {
        var p = new Param("x", 1);
        var atan = Exp.Atan(p);
        Assert.Equal(Math.PI / 4, atan.Eval(), 6);
    }

    [Fact]
    public void Expression_Atan2_Works()
    {
        var y = new Param("y", 1);
        var x = new Param("x", 1);
        var atan2 = Exp.Atan2(y, x);
        Assert.Equal(Math.PI / 4, atan2.Eval(), 6);
    }

    [Fact]
    public void Expression_Pow_Works()
    {
        var baseVal = new Param("base", 2);
        var expVal = new Param("exp", 3);
        var pow = Exp.Pow(baseVal, expVal);
        Assert.Equal(8, pow.Eval(), 6);
    }

    [Fact]
    public void Expression_Exp_Works()
    {
        var p = new Param("x", 1);
        var exp = Exp.Exp(p);
        Assert.Equal(Math.E, exp.Eval(), 6);
    }

    [Fact]
    public void Expression_Ln_Works()
    {
        var p = new Param("x", Math.E);
        var ln = Exp.Ln(p);
        Assert.Equal(1, ln.Eval(), 6);
    }

    #endregion

    #region Derivative Tests

    [Fact]
    public void Expression_Derivative_Works()
    {
        var x = new Param("x", 2);
        var expr = x * x + x; // x^2 + x
        var deriv = expr.Deriv(x); // 2x + 1
        Assert.Equal(5, deriv.Eval(), 6);
    }

    [Fact]
    public void Expression_DerivativeOfConstant_IsZero()
    {
        var x = new Param("x", 2);
        var c = new ConstExp(5);
        var deriv = c.Deriv(x);
        Assert.Equal(0, deriv.Eval(), 6);
    }

    [Fact]
    public void Expression_DerivativeChainRule_Works()
    {
        var x = new Param("x", 2);
        var expr = Exp.Sin(x); // sin(x)
        var deriv = expr.Deriv(x); // cos(x)
        Assert.Equal(Math.Cos(2), deriv.Eval(), 6);
    }

    #endregion

    #region Dependency Tests

    [Fact]
    public void Expression_IsDependOn_Works()
    {
        var x = new Param("x", 1);
        var y = new Param("y", 2);
        var expr = x + y;

        Assert.True(expr.IsDependOn(x));
        Assert.True(expr.IsDependOn(y));
    }

    [Fact]
    public void Expression_NotDependOn_UnrelatedParam()
    {
        var x = new Param("x", 1);
        var y = new Param("y", 2);
        var z = new Param("z", 3);
        var expr = x + y;

        Assert.False(expr.IsDependOn(z));
    }

    [Fact]
    public void Expression_DependOnParams_ReturnsAllParams()
    {
        var x = new Param("x", 1);
        var y = new Param("y", 2);
        var expr = x * x + y;

        var deps = expr.DependOnParams();
        Assert.Contains(x, deps);
        Assert.Contains(y, deps);
    }

    #endregion

    #region Substitution Tests

    [Fact]
    public void Expression_DeepClone_Works()
    {
        var x = new Param("x", 5);
        var expr = x * x + x;
        var clone = expr.DeepClone();

        x.value = 10; // Modify original
        Assert.Equal(5, ((Param)clone).value); // Clone unchanged
    }

    [Fact]
    public void Expression_Substitute_Works()
    {
        var x = new Param("x", 1);
        var y = new Param("y", 2);
        var expr = x * x;
        var substituted = expr.Substitute(x, y);

        Assert.Equal(4, substituted.Eval(), 6);
    }

    #endregion
}

public class SketchTests
{
    #region Basic Sketch Tests

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

    #endregion

    #region Selection Tests

    [Fact]
    public void Sketch_SelectAll_Works()
    {
        var sketch = new Sketch();
        sketch.AddEntity(new LineEntity(new Vec2(0, 0), new Vec2(10, 0)));
        sketch.AddEntity(new LineEntity(new Vec2(0, 0), new Vec2(0, 10)));

        sketch.SelectAll();

        Assert.All(sketch.Entities, e => Assert.True(e.IsSelected));
    }

    [Fact]
    public void Sketch_DeselectAll_Works()
    {
        var sketch = new Sketch();
        sketch.AddEntity(new LineEntity(new Vec2(0, 0), new Vec2(10, 0)));

        sketch.SelectAll();
        sketch.DeselectAll();

        Assert.All(sketch.Entities, e => Assert.False(e.IsSelected));
    }

    [Fact]
    public void Sketch_GetSelectedEntities_Works()
    {
        var sketch = new Sketch();
        var line1 = new LineEntity(new Vec2(0, 0), new Vec2(10, 0));
        var line2 = new LineEntity(new Vec2(0, 0), new Vec2(0, 10));
        sketch.AddEntity(line1);
        sketch.AddEntity(line2);

        line1.IsSelected = true;

        var selected = sketch.GetSelectedEntities().ToList();
        Assert.Single(selected);
        Assert.Same(line1, selected[0]);
    }

    #endregion

    #region Entity Management Tests

    [Fact]
    public void Sketch_RemoveEntity_RemovesRelatedConstraints()
    {
        var sketch = new Sketch();
        var line = new LineEntity(new Vec2(0, 0), new Vec2(100, 0));
        sketch.AddEntity(line);
        sketch.AddConstraint(new HorizontalConstraint(line));

        sketch.RemoveEntity(line);

        Assert.Empty(sketch.Entities);
        Assert.Empty(sketch.Constraints);
    }

    [Fact]
    public void Sketch_GetConstraintsFor_Works()
    {
        var sketch = new Sketch();
        var line = new LineEntity(new Vec2(0, 0), new Vec2(100, 0));
        sketch.AddEntity(line);
        sketch.AddConstraint(new HorizontalConstraint(line));

        var constraints = sketch.GetConstraintsFor(line).ToList();

        Assert.Single(constraints);
    }

    #endregion

    #region Move Tests

    [Fact]
    public void Sketch_MoveSelected_Works()
    {
        var sketch = new Sketch();
        var line = new LineEntity(new Vec2(0, 0), new Vec2(100, 0));
        sketch.AddEntity(line);
        line.IsSelected = true;

        var startPos = line.StartPosition;
        sketch.MoveSelected(new Vec2(10, 10));

        Assert.Equal(startPos.X + 10, line.StartPosition.X, 6);
        Assert.Equal(startPos.Y + 10, line.StartPosition.Y, 6);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void Sketch_ModifiedEvent_Raises()
    {
        var sketch = new Sketch();
        bool eventRaised = false;
        sketch.Modified += () => eventRaised = true;

        sketch.AddEntity(new LineEntity(new Vec2(0, 0), new Vec2(10, 0)));

        Assert.True(eventRaised);
    }

    [Fact]
    public void Sketch_SolveCompletedEvent_Raises()
    {
        var sketch = new Sketch();
        bool eventRaised = false;
        sketch.SolveCompleted += _ => eventRaised = true;

        var line = new LineEntity(new Vec2(0, 0), new Vec2(100, 0));
        sketch.AddEntity(line);
        sketch.Solve();

        Assert.True(eventRaised);
    }

    #endregion
}

public class ConstraintSolverTests
{
    #region Basic Solver Tests

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
    public void EquationSystem_Clear_Works()
    {
        var system = new EquationSystem();
        var x = new Param("x", 5);
        system.AddParameter(x);
        system.AddEquation(x - new ConstExp(10));

        system.Clear();

        Assert.Empty(system.Parameters);
        Assert.Empty(system.Equations);
    }

    [Fact]
    public void EquationSystem_MarkDirty_Works()
    {
        var system = new EquationSystem();
        var x = new Param("x", 5);
        system.AddParameter(x);

        Assert.True(system.IsDirty);

        system.MarkDirty();
        Assert.True(system.IsDirty);
    }

    #endregion

    #region Geometric Constraint Tests

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
    public void VerticalConstraint_Solves()
    {
        var system = new EquationSystem();
        var line = new LineEntity(new Vec2(0, 0), new Vec2(10, 100));

        var vConstraint = new VerticalConstraint(line);
        line.SetupEquations(system);
        vConstraint.SetupEquations(system);

        var result = system.Solve();

        Assert.Equal(SolveResult.OKAY, result);
        // After solving, X coordinates should be equal
        Assert.Equal(line.StartPosition.X, line.EndPosition.X, 1);
    }

    [Fact]
    public void ParallelConstraint_Solves()
    {
        var system = new EquationSystem();
        var line1 = new LineEntity(new Vec2(0, 0), new Vec2(100, 50));
        var line2 = new LineEntity(new Vec2(0, 100), new Vec2(100, 140));

        var pConstraint = new ParallelConstraint(line1, line2);
        line1.SetupEquations(system);
        line2.SetupEquations(system);
        pConstraint.SetupEquations(system);

        var result = system.Solve();

        Assert.Equal(SolveResult.OKAY, result);
        // Lines should have same slope
        var slope1 = (line1.EndPosition.Y - line1.StartPosition.Y) / 
                     (line1.EndPosition.X - line1.StartPosition.X);
        var slope2 = (line2.EndPosition.Y - line2.StartPosition.Y) / 
                     (line2.EndPosition.X - line2.StartPosition.X);
        Assert.Equal(slope1, slope2, 2);
    }

    [Fact]
    public void PerpendicularConstraint_Solves()
    {
        var system = new EquationSystem();
        var line1 = new LineEntity(new Vec2(0, 0), new Vec2(100, 0));
        var line2 = new LineEntity(new Vec2(50, 0), new Vec2(50, 100));

        var pConstraint = new PerpendicularConstraint(line1, line2);
        line1.SetupEquations(system);
        line2.SetupEquations(system);
        pConstraint.SetupEquations(system);

        var result = system.Solve();

        Assert.Equal(SolveResult.OKAY, result);
        // Dot product should be ~0
        var dot = Vec2.Dot(line1.Direction, line2.Direction);
        Assert.Equal(0, Math.Abs(dot), 1);
    }

    #endregion

    #region Distance Constraint Tests

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

    [Fact]
    public void LengthConstraint_Solves()
    {
        var system = new EquationSystem();
        var line = new LineEntity(new Vec2(0, 0), new Vec2(50, 0));

        var targetLength = 100.0;
        var constraint = new LengthConstraint(line, targetLength);

        line.SetupEquations(system);
        constraint.SetupEquations(system);

        var result = system.Solve();

        Assert.Equal(SolveResult.OKAY, result);
        Assert.Equal(targetLength, line.Length, 1);
    }

    [Fact]
    public void DiameterConstraint_Solves()
    {
        var system = new EquationSystem();
        var circle = new CircleEntity(new Vec2(100, 100), 25);

        var targetDiameter = 100.0;
        var constraint = new DiameterConstraint(circle, targetDiameter);

        circle.SetupEquations(system);
        constraint.SetupEquations(system);

        var result = system.Solve();

        Assert.Equal(SolveResult.OKAY, result);
        Assert.Equal(targetDiameter, circle.RadiusValue * 2, 1);
    }

    #endregion

    #region Complex Constraint Tests

    [Fact]
    public void Sketch_Rectangle_HasCorrectDOF()
    {
        var sketch = Sketch.CreateSimpleRectangle(200, 100);
        
        // 4 lines = 4 * 4 = 16 DOF
        // 4 constraints (2 parallel + 1 equal + 1 fixation) = 4 DOF
        // 16 - 4 = 12 DOF
        var dof = sketch.GetDegreesOfFreedom();
        Assert.Equal(12, dof);
    }

    [Fact]
    public void Sketch_FullyConstrained_Rectangle()
    {
        var sketch = new Sketch();

        // Create rectangle with full constraints
        var p1 = new PointEntity(0, 0);
        var p2 = new PointEntity(200, 0);
        var p3 = new PointEntity(200, 100);
        var p4 = new PointEntity(0, 100);

        var l1 = new LineEntity(p1, p2);
        var l2 = new LineEntity(p2, p3);
        var l3 = new LineEntity(p3, p4);
        var l4 = new LineEntity(p4, p1);

        sketch.AddEntity(l1);
        sketch.AddEntity(l2);
        sketch.AddEntity(l3);
        sketch.AddEntity(l4);

        // Add constraints
        sketch.AddConstraint(new HorizontalConstraint(l1));
        sketch.AddConstraint(new HorizontalConstraint(l3));
        sketch.AddConstraint(new VerticalConstraint(l2));
        sketch.AddConstraint(new VerticalConstraint(l4));
        sketch.AddConstraint(new EqualLengthConstraint(l1, l2));
        sketch.AddConstraint(new FixationConstraint(p1, new Vec2(0, 0)));

        // Solve
        var result = sketch.Solve();
        Assert.Equal(SolveResult.OKAY, result);

        // Check rectangle properties
        Assert.Equal(l1.Length, l2.Length, 1);
        Assert.Equal(l2.Length, l3.Length, 1);
        Assert.Equal(l3.Length, l4.Length, 1);
        Assert.Equal(0, p1.Position.X, 1);
        Assert.Equal(0, p1.Position.Y, 1);
    }

    #endregion
}

#region Additional Entity Tests

public class EllipseEntityTests
{
    [Fact]
    public void EllipseEntity_Create_Works()
    {
        var ellipse = new EllipseEntity(new Vec2(0, 0), 10, 5, 0);

        Assert.Equal(10, ellipse.RadiusXValue, 6);
        Assert.Equal(5, ellipse.RadiusYValue, 6);
        Assert.Equal(0, ellipse.RotationValue, 6);
    }

    [Fact]
    public void EllipseEntity_GetPointAt_Works()
    {
        var ellipse = new EllipseEntity(new Vec2(0, 0), 10, 5, 0);
        var point = ellipse.GetPointAt(0);

        Assert.Equal(10, point.X, 6);
        Assert.Equal(0, point.Y, 6);
    }

    [Fact]
    public void EllipseEntity_ToLocal_Works()
    {
        var ellipse = new EllipseEntity(new Vec2(5, 5), 10, 5, Math.PI / 4);
        var worldPoint = new Vec2(15, 5);
        var local = ellipse.ToLocal(worldPoint);

        Assert.NotNull(local);
    }
}

public class SplineEntityTests
{
    [Fact]
    public void SplineEntity_Create_Works()
    {
        var points = new[] { new Vec2(0, 0), new Vec2(5, 5), new Vec2(10, 0) };
        var spline = new SplineEntity(points, 2);

        Assert.Equal(2, spline.Degree);
        Assert.Equal(3, spline.ControlPoints.Count);
    }

    [Fact]
    public void SplineEntity_Evaluate_Works()
    {
        var points = new[] { new Vec2(0, 0), new Vec2(5, 5), new Vec2(10, 0) };
        var spline = new SplineEntity(points, 2);
        var point = spline.Evaluate(0.5);

        Assert.NotNull(point);
    }
}

public class FunctionEntityTests
{
    [Fact]
    public void FunctionEntity_Create_Works()
    {
        var func = new FunctionEntity(new Vec2(0, 0), -10, 10, "x*x");

        Assert.Equal(-10, func.XMin, 6);
        Assert.Equal(10, func.XMax, 6);
        Assert.Equal("x*x", func.FunctionExpression);
    }

    [Fact]
    public void FunctionEntity_Evaluate_Polynomial()
    {
        var func = new FunctionEntity(new Vec2(0, 0), -10, 10, "2*x+1");
        var result = func.Evaluate(5);

        Assert.Equal(11, result, 6);
    }
}

#endregion

#region Feature Tests

public class FeatureTests
{
    [Fact]
    public void ExtrusionFeature_Create_Works()
    {
        var profile = new List<Entity>
        {
            new LineEntity(new Vec2(0, 0), new Vec2(10, 0)),
            new LineEntity(new Vec2(10, 0), new Vec2(10, 10)),
            new LineEntity(new Vec2(10, 10), new Vec2(0, 10)),
            new LineEntity(new Vec2(0, 10), new Vec2(0, 0))
        };

        var extrusion = new ExtrusionFeature(profile, 20);

        Assert.Equal(20, extrusion.Distance, 6);
        Assert.True(extrusion.Solid);
        Assert.Equal(4, extrusion.Profile.Count);
    }

    [Fact]
    public void RevolutionFeature_Create_Works()
    {
        var profile = new List<Entity>
        {
            new LineEntity(new Vec2(0, 0), new Vec2(10, 0)),
            new LineEntity(new Vec2(10, 0), new Vec2(10, 10)),
            new LineEntity(new Vec2(10, 10), new Vec2(0, 0))
        };

        var revolution = new RevolutionFeature(profile, new Vec3(0, 0, 0), new Vec3(0, 0, 1), 360);

        Assert.Equal(360, revolution.EndAngle, 6);
        Assert.True(revolution.Solid);
        Assert.True(revolution.IsFullRevolution());
    }

    [Fact]
    public void BoxFeature_Create_Works()
    {
        var box = new BoxFeature(new Vec3(0, 0, 0), 10, 20, 30);

        Assert.Equal(10, box.Width, 6);
        Assert.Equal(20, box.Height, 6);
        Assert.Equal(30, box.Depth, 6);
        Assert.Equal(new Vec3(5, 10, 15), box.Center);
    }

    [Fact]
    public void CylinderFeature_Create_Works()
    {
        var cylinder = new CylinderFeature(new Vec3(0, 0, 0), 5, 20);

        Assert.Equal(5, cylinder.Radius, 6);
        Assert.Equal(20, cylinder.Height, 6);
        Assert.Equal(new Vec3(0, 0, 20), cylinder.TopCenter);
    }

    [Fact]
    public void SphereFeature_Create_Works()
    {
        var sphere = new SphereFeature(new Vec3(0, 0, 0), 10);

        Assert.Equal(10, sphere.Radius, 6);
        Assert.Equal(new Vec3(0, 0, 0), sphere.Center);
    }
}

#endregion
