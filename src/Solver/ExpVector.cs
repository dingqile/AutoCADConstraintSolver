namespace AutoCADConstraintSolver.Solver;

/// <summary>
/// 3D vector expression for geometric calculations
/// </summary>
public class ExpVector
{
    public Exp x { get; set; }
    public Exp y { get; set; }
    public Exp z { get; set; }

    public ExpVector(Exp x, Exp y, Exp z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public ExpVector(double x, double y, double z)
    {
        this.x = new ConstExp(x);
        this.y = new ConstExp(y);
        this.z = new ConstExp(z);
    }

    public static ExpVector Zero => new(0, 0, 0);
    public static ExpVector UnitX => new(1, 0, 0);
    public static ExpVector UnitY => new(0, 1, 0);
    public static ExpVector UnitZ => new(0, 0, 1);

    public Exp MagnitudeSquared => x * x + y * y + z * z;
    public Exp Magnitude => Exp.Sqrt(MagnitudeSquared);

    public ExpVector Normalized => this / Magnitude;

    public static Exp Dot(ExpVector a, ExpVector b) => a.x * b.x + a.y * b.y + a.z * b.z;

    public static ExpVector Cross(ExpVector a, ExpVector b) => new(
        a.y * b.z - a.z * b.y,
        a.z * b.x - a.x * b.z,
        a.x * b.y - a.y * b.x);

    public static ExpVector operator +(ExpVector a, ExpVector b) =>
        new(a.x + b.x, a.y + b.y, a.z + b.z);

    public static ExpVector operator -(ExpVector a, ExpVector b) =>
        new(a.x - b.x, a.y - b.y, a.z - b.z);

    public static ExpVector operator *(ExpVector a, Exp scalar) =>
        new(a.x * scalar, a.y * scalar, a.z * scalar);

    public static ExpVector operator *(Exp scalar, ExpVector a) =>
        new(a.x * scalar, a.y * scalar, a.z * scalar);

    public static ExpVector operator /(ExpVector a, Exp scalar) =>
        new(a.x / scalar, a.y / scalar, a.z / scalar);

    public static ExpVector operator -(ExpVector a) =>
        new(-a.x, -a.y, -a.z);

    public ExpVector DeepClone() => new(x.DeepClone(), y.DeepClone(), z.DeepClone());

    public HashSet<Param> DependOnParams()
    {
        var result = x.DependOnParams();
        result.UnionWith(y.DependOnParams());
        result.UnionWith(z.DependOnParams());
        return result;
    }

    public override string ToString() => $"({x:F4}, {y:F4}, {z:F4})";
}

/// <summary>
/// 2D vector expression
/// </summary>
public class ExpVector2d
{
    public Exp x { get; set; }
    public Exp y { get; set; }

    public ExpVector2d(Exp x, Exp y)
    {
        this.x = x;
        this.y = y;
    }

    public ExpVector2d(double x, double y)
    {
        this.x = new ConstExp(x);
        this.y = new ConstExp(y);
    }

    public static ExpVector2d Zero => new(0, 0);
    public static ExpVector2d UnitX => new(1, 0);
    public static ExpVector2d UnitY => new(0, 1);

    public Exp MagnitudeSquared => x * x + y * y;
    public Exp Magnitude => Exp.Sqrt(MagnitudeSquared);

    public ExpVector2d Normalized => this / Magnitude;

    public static Exp Dot(ExpVector2d a, ExpVector2d b) => a.x * b.x + a.y * b.y;

    public static Exp Cross(ExpVector2d a, ExpVector2d b) => a.x * b.y - a.y * b.x;

    public static ExpVector2d operator +(ExpVector2d a, ExpVector2d b) =>
        new(a.x + b.x, a.y + b.y);

    public static ExpVector2d operator -(ExpVector2d a, ExpVector2d b) =>
        new(a.x - b.x, a.y - b.y);

    public static ExpVector2d operator *(ExpVector2d a, Exp scalar) =>
        new(a.x * scalar, a.y * scalar);

    public static ExpVector2d operator *(Exp scalar, ExpVector2d a) =>
        new(a.x * scalar, a.y * scalar);

    public static ExpVector2d operator /(ExpVector2d a, Exp scalar) =>
        new(a.x / scalar, a.y / scalar);

    public static ExpVector2d operator -(ExpVector2d a) =>
        new(-a.x, -a.y);

    public ExpVector ToExpVector() => new(x, y, Exp.zero);

    public ExpVector2d DeepClone() => new(x.DeepClone(), y.DeepClone());

    public HashSet<Param> DependOnParams()
    {
        var result = x.DependOnParams();
        result.UnionWith(y.DependOnParams());
        return result;
    }

    public override string ToString() => $"({x:F4}, {y:F4})";
}
