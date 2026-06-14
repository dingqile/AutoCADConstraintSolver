namespace AutoCADConstraintSolver.Solver;

/// <summary>
/// Base class for all expressions in the constraint solver
/// </summary>
public abstract class Exp
{
    public static readonly Exp zero = new ConstExp(0);
    public static readonly Exp one = new ConstExp(1);
    public static readonly Exp two = new ConstExp(2);
    public static readonly Exp pi = new ConstExp(Math.PI);
    public static readonly Exp twoPi = new ConstExp(2 * Math.PI);

    public abstract double Eval();
    public abstract Exp Deriv(Param p);
    public abstract bool IsDependOn(Param p);
    public abstract HashSet<Param> DependOnParams();
    public abstract Exp DeepClone();
    public abstract bool IsSubstitionForm();
    public abstract Exp Substitute(Param oldParam, Param newParam);

    public static Exp operator +(Exp a, Exp b) => new AddExp(a, b);
    public static Exp operator -(Exp a, Exp b) => new SubExp(a, b);
    public static Exp operator *(Exp a, Exp b) => new MulExp(a, b);
    public static Exp operator /(Exp a, Exp b) => new DivExp(a, b);
    public static Exp operator -(Exp a) => new NegExp(a);

    public static Exp Sqrt(Exp e) => new SqrtExp(e);
    public static Exp Abs(Exp e) => new AbsExp(e);
    public static Exp Sin(Exp e) => new SinExp(e);
    public static Exp Cos(Exp e) => new CosExp(e);
    public static Exp Tan(Exp e) => new TanExp(e);
    public static Exp Asin(Exp e) => new AsinExp(e);
    public static Exp Acos(Exp e) => new AcosExp(e);
    public static Exp Atan(Exp e) => new AtanExp(e);
    public static Exp Atan2(Exp y, Exp x) => new Atan2Exp(y, x);
    public static Exp Exp(Exp e) => new ExpFuncExp(e);
    public static Exp Ln(Exp e) => new LnExp(e);
    public static Exp Pow(Exp a, Exp b) => new PowExp(a, b);
}

public class Param : Exp
{
    public double value;
    public readonly string Name;

    public Param(string name, double value = 0)
    {
        Name = name;
        this.value = value;
    }

    public override double Eval() => value;

    public override Exp Deriv(Param p) => this == p ? one : zero;

    public override bool IsDependOn(Param p) => this == p;

    public override HashSet<Param> DependOnParams() => new() { this };

    public override Exp DeepClone() => new Param(Name, value);

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        this == oldParam ? newParam : this;

    public override string ToString() => $"{Name}={value:F4}";
}

internal class ConstExp : Exp
{
    private readonly double _value;

    public ConstExp(double value) => _value = value;

    public override double Eval() => _value;

    public override Exp Deriv(Param p) => zero;

    public override bool IsDependOn(Param p) => false;

    public override HashSet<Param> DependOnParams() => new();

    public override Exp DeepClone() => this;

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => this;

    public override string ToString() => _value.ToString("F4");
}

internal class AddExp : Exp
{
    public Exp a, b;

    public AddExp(Exp a, Exp b)
    {
        this.a = a;
        this.b = b;
    }

    public override double Eval() => a.Eval() + b.Eval();

    public override Exp Deriv(Param p) => a.Deriv(p) + b.Deriv(p);

    public override bool IsDependOn(Param p) => a.IsDependOn(p) || b.IsDependOn(p);

    public override HashSet<Param> DependOnParams()
    {
        var result = a.DependOnParams();
        result.UnionWith(b.DependOnParams());
        return result;
    }

    public override Exp DeepClone() => new AddExp(a.DeepClone(), b.DeepClone());

    public override bool IsSubstitionForm() => a.IsSubstitionForm() && b.IsSubstitionForm();

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new AddExp(a.Substitute(oldParam, newParam), b.Substitute(oldParam, newParam));

    public override string ToString() => $"({a} + {b})";
}

internal class SubExp : Exp
{
    public Exp a, b;

    public SubExp(Exp a, Exp b)
    {
        this.a = a;
        this.b = b;
    }

    public override double Eval() => a.Eval() - b.Eval();

    public override Exp Deriv(Param p) => a.Deriv(p) - b.Deriv(p);

    public override bool IsDependOn(Param p) => a.IsDependOn(p) || b.IsDependOn(p);

    public override HashSet<Param> DependOnParams()
    {
        var result = a.DependOnParams();
        result.UnionWith(b.DependOnParams());
        return result;
    }

    public override Exp DeepClone() => new SubExp(a.DeepClone(), b.DeepClone());

    public override bool IsSubstitionForm() => a.IsSubstitionForm() && b.IsSubstitionForm();

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new SubExp(a.Substitute(oldParam, newParam), b.Substitute(oldParam, newParam));

    public override string ToString() => $"({a} - {b})";
}

internal class MulExp : Exp
{
    public Exp a, b;

    public MulExp(Exp a, Exp b)
    {
        this.a = a;
        this.b = b;
    }

    public override double Eval() => a.Eval() * b.Eval();

    public override Exp Deriv(Param p) => a.Deriv(p) * b + a * b.Deriv(p);

    public override bool IsDependOn(Param p) => a.IsDependOn(p) || b.IsDependOn(p);

    public override HashSet<Param> DependOnParams()
    {
        var result = a.DependOnParams();
        result.UnionWith(b.DependOnParams());
        return result;
    }

    public override Exp DeepClone() => new MulExp(a.DeepClone(), b.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new MulExp(a.Substitute(oldParam, newParam), b.Substitute(oldParam, newParam));

    public override string ToString() => $"({a} * {b})";
}

internal class DivExp : Exp
{
    public Exp a, b;

    public DivExp(Exp a, Exp b)
    {
        this.a = a;
        this.b = b;
    }

    public override double Eval() => a.Eval() / b.Eval();

    public override Exp Deriv(Param p) => (a.Deriv(p) * b - a * b.Deriv(p)) / (b * b);

    public override bool IsDependOn(Param p) => a.IsDependOn(p) || b.IsDependOn(p);

    public override HashSet<Param> DependOnParams()
    {
        var result = a.DependOnParams();
        result.UnionWith(b.DependOnParams());
        return result;
    }

    public override Exp DeepClone() => new DivExp(a.DeepClone(), b.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new DivExp(a.Substitute(oldParam, newParam), b.Substitute(oldParam, newParam));

    public override string ToString() => $"({a} / {b})";
}

internal class NegExp : Exp
{
    public Exp e;

    public NegExp(Exp e) => this.e = e;

    public override double Eval() => -e.Eval();

    public override Exp Deriv(Param p) => -(e.Deriv(p));

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new NegExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new NegExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"-({e})";
}

internal class SqrtExp : Exp
{
    public Exp e;

    public SqrtExp(Exp e) => this.e = e;

    public override double Eval() => Math.Sqrt(Math.Max(0, e.Eval()));

    public override Exp Deriv(Param p) => e.Deriv(p) / (two * Sqrt(e));

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new SqrtExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new SqrtExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"sqrt({e})";
}

internal class AbsExp : Exp
{
    public Exp e;

    public AbsExp(Exp e) => this.e = e;

    public override double Eval() => Math.Abs(e.Eval());

    public override Exp Deriv(Param p) => (e / Abs(e)) * e.Deriv(p);

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new AbsExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new AbsExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"abs({e})";
}

internal class SinExp : Exp
{
    public Exp e;

    public SinExp(Exp e) => this.e = e;

    public override double Eval() => Math.Sin(e.Eval());

    public override Exp Deriv(Param p) => Cos(e) * e.Deriv(p);

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new SinExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new SinExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"sin({e})";
}

internal class CosExp : Exp
{
    public Exp e;

    public CosExp(Exp e) => this.e = e;

    public override double Eval() => Math.Cos(e.Eval());

    public override Exp Deriv(Param p) => -Sin(e) * e.Deriv(p);

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new CosExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new CosExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"cos({e})";
}

internal class TanExp : Exp
{
    public Exp e;

    public TanExp(Exp e) => this.e = e;

    public override double Eval() => Math.Tan(e.Eval());

    public override Exp Deriv(Param p) => e.Deriv(p) / (Cos(e) * Cos(e));

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new TanExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new TanExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"tan({e})";
}

internal class AsinExp : Exp
{
    public Exp e;

    public AsinExp(Exp e) => this.e = e;

    public override double Eval() => Math.Asin(Math.Clamp(e.Eval(), -1, 1));

    public override Exp Deriv(Param p) => e.Deriv(p) / Sqrt(one - e * e);

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new AsinExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new AsinExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"asin({e})";
}

internal class AcosExp : Exp
{
    public Exp e;

    public AcosExp(Exp e) => this.e = e;

    public override double Eval() => Math.Acos(Math.Clamp(e.Eval(), -1, 1));

    public override Exp Deriv(Param p) => -e.Deriv(p) / Sqrt(one - e * e);

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new AcosExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new AcosExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"acos({e})";
}

internal class AtanExp : Exp
{
    public Exp e;

    public AtanExp(Exp e) => this.e = e;

    public override double Eval() => Math.Atan(e.Eval());

    public override Exp Deriv(Param p) => e.Deriv(p) / (one + e * e);

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new AtanExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new AtanExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"atan({e})";
}

internal class Atan2Exp : Exp
{
    public Exp y, x;

    public Atan2Exp(Exp y, Exp x)
    {
        this.y = y;
        this.x = x;
    }

    public override double Eval() => Math.Atan2(y.Eval(), x.Eval());

    public override Exp Deriv(Param p)
    {
        var x2 = x * x;
        var y2 = y * y;
        var sum = x2 + y2;
        return (x * y.Deriv(p) - y * x.Deriv(p)) / sum;
    }

    public override bool IsDependOn(Param p) => x.IsDependOn(p) || y.IsDependOn(p);

    public override HashSet<Param> DependOnParams()
    {
        var result = x.DependOnParams();
        result.UnionWith(y.DependOnParams());
        return result;
    }

    public override Exp DeepClone() => new Atan2Exp(y.DeepClone(), x.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new Atan2Exp(y.Substitute(oldParam, newParam), x.Substitute(oldParam, newParam));

    public override string ToString() => $"atan2({y}, {x})";
}

internal class ExpFuncExp : Exp
{
    public Exp e;

    public ExpFuncExp(Exp e) => this.e = e;

    public override double Eval() => Math.Exp(e.Eval());

    public override Exp Deriv(Param p) => Exp(e) * e.Deriv(p);

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new ExpFuncExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new ExpFuncExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"exp({e})";
}

internal class LnExp : Exp
{
    public Exp e;

    public LnExp(Exp e) => this.e = e;

    public override double Eval() => Math.Log(Math.Max(1e-10, e.Eval()));

    public override Exp Deriv(Param p) => e.Deriv(p) / e;

    public override bool IsDependOn(Param p) => e.IsDependOn(p);

    public override HashSet<Param> DependOnParams() => e.DependOnParams();

    public override Exp DeepClone() => new LnExp(e.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new LnExp(e.Substitute(oldParam, newParam));

    public override string ToString() => $"ln({e})";
}

internal class PowExp : Exp
{
    public Exp a, b;

    public PowExp(Exp a, Exp b)
    {
        this.a = a;
        this.b = b;
    }

    public override double Eval()
    {
        var aval = a.Eval();
        var bval = b.Eval();
        return Math.Pow(Math.Max(0, aval), bval);
    }

    public override Exp Deriv(Param p)
    {
        if (b.IsDependOn(p))
        {
            return this * (b.Deriv(p) * Ln(a) + b * a.Deriv(p) / a);
        }
        return b * Pow(a, b - Exp.one) * a.Deriv(p);
    }

    public override bool IsDependOn(Param p) => a.IsDependOn(p) || b.IsDependOn(p);

    public override HashSet<Param> DependOnParams()
    {
        var result = a.DependOnParams();
        result.UnionWith(b.DependOnParams());
        return result;
    }

    public override Exp DeepClone() => new PowExp(a.DeepClone(), b.DeepClone());

    public override bool IsSubstitionForm() => false;

    public override Exp Substitute(Param oldParam, Param newParam) => 
        new PowExp(a.Substitute(oldParam, newParam), b.Substitute(oldParam, newParam));

    public override string ToString() => $"pow({a}, {b})";
}

/// <summary>
/// Equality constraint: a = b (substitution form)
/// </summary>
internal class EqExp : Exp
{
    public Param a, b;

    public EqExp(Param a, Param b)
    {
        this.a = a;
        this.b = b;
    }

    public override double Eval() => a.value - b.value;

    public override Exp Deriv(Param p)
    {
        if (a == p) return Exp.one;
        if (b == p) return -Exp.one;
        return Exp.zero;
    }

    public override bool IsDependOn(Param p) => a == p || b == p;

    public override HashSet<Param> DependOnParams() => new() { a, b };

    public override Exp DeepClone() => new EqExp(a, b);

    public override bool IsSubstitionForm() => true;

    public override Exp Substitute(Param oldParam, Param newParam)
    {
        return new EqExp(
            a == oldParam ? (Param)newParam.DeepClone() : a,
            b == oldParam ? (Param)newParam.DeepClone() : b);
    }

    public override string ToString() => $"{a} = {b}";
}
