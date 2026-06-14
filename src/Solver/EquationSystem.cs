using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoCADConstraintSolver.Solver;

/// <summary>
/// Result of the constraint solver
/// </summary>
public enum SolveResult
{
    /// <summary>Solver converged successfully</summary>
    OKAY,
    
    /// <summary>System didn't converge after max iterations</summary>
    DIDNT_CONVEGE,
    
    /// <summary>System has redundant constraints</summary>
    REDUNDANT,
    
    /// <summary>Solver postponed due to dependencies</summary>
    POSTPONE,
    
    /// <summary>Internal solver failure</summary>
    INTERNAL_FAILURE,
    
    /// <summary>Solution jumped unexpectedly</summary>
    JUMP
}

/// <summary>
/// Algebraic constraint solver using Newton-Raphson iteration
/// Based on the NoteCAD constraint solver
/// </summary>
public class EquationSystem
{
    private const double Epsilon = 1e-6;
    
    /// <summary>Maximum solver iterations</summary>
    public int MaxSteps { get; set; } = 20;
    
    /// <summary>Steps to take when dragging</summary>
    public int DragSteps { get; set; } = 3;
    
    /// <summary>Whether to revert when not converged</summary>
    public bool RevertWhenNotConverged { get; set; } = true;
    
    /// <summary>Whether to avoid solution jumping</summary>
    public bool AvoidJumping { get; set; } = false;
    
    /// <summary>Jump factor when avoiding jumps</summary>
    public double JumpFactor { get; set; } = 20.0;
    
    /// <summary>Number of perturbation steps for stuck solutions</summary>
    public int PerturbationSteps { get; set; } = 0;

    private Exp[,]? _jacobian;
    private List<int>[]? _nzColumns;
    private List<int>[]? _nzRows;
    private double[,]? _a;
    private double[,]? _aat;
    private double[]? _b;
    private double[]? _x;
    private double[]? _z;
    private double[]? _oldParamValues;

    private readonly List<Exp> _sourceEquations = new();
    private readonly List<Param> _parameters = new();
    private List<Exp> _equations = new();
    private List<Param> _currentParams = new();

    /// <summary>Whether the system needs rebuilding</summary>
    public bool IsDirty { get; private set; } = true;

    /// <summary>All equations in the system</summary>
    public IEnumerable<Exp> Equations => _sourceEquations.AsEnumerable();

    /// <summary>All parameters in the system</summary>
    public IEnumerable<Param> Parameters => _parameters.AsEnumerable();

    /// <summary>Add an equation to the system</summary>
    public void AddEquation(Exp eq)
    {
        _sourceEquations.Add(eq);
        IsDirty = true;
    }

    /// <summary>Add a vector equation (adds x and y components)</summary>
    public void AddEquation(ExpVector v)
    {
        _sourceEquations.Add(v.x);
        _sourceEquations.Add(v.y);
        if (!IsZero(v.z))
            _sourceEquations.Add(v.z);
        IsDirty = true;
    }

    /// <summary>Add multiple equations</summary>
    public void AddEquations(IEnumerable<Exp> eq)
    {
        _sourceEquations.AddRange(eq);
        IsDirty = true;
    }

    /// <summary>Remove an equation from the system</summary>
    public void RemoveEquation(Exp eq)
    {
        _sourceEquations.Remove(eq);
        IsDirty = true;
    }

    /// <summary>Add a parameter to the system</summary>
    public void AddParameter(Param p)
    {
        _parameters.Add(p);
        IsDirty = true;
    }

    /// <summary>Add multiple parameters</summary>
    public void AddParameters(IEnumerable<Param> p)
    {
        _parameters.AddRange(p);
        IsDirty = true;
    }

    /// <summary>Remove a parameter from the system</summary>
    public void RemoveParameter(Param p)
    {
        _parameters.Remove(p);
        IsDirty = true;
    }

    /// <summary>Get current number of parameters</summary>
    public int CurrentParamsCount() => _currentParams.Count;

    /// <summary>Get current number of equations</summary>
    public int CurrentEquationsCount() => _equations.Count;

    /// <summary>Check if the system has any drag constraints</summary>
    public bool HasDragged() => _equations.Any(e => e is IDragConstraint);

    /// <summary>Evaluate all equations</summary>
    public void Eval(ref double[] b, bool clearDrag)
    {
        for (int i = 0; i < _equations.Count; i++)
        {
            if (clearDrag && _equations[i] is IDragConstraint dc && dc.IsDrag)
            {
                b[i] = 0.0;
                continue;
            }
            b[i] = _equations[i].Eval();
            if (double.IsNaN(b[i]))
            {
                b[i] = 0.0;
            }
        }
    }

    /// <summary>Check if the solution has converged</summary>
    public bool IsConverged(bool checkDrag = true)
    {
        for (int i = 0; i < _equations.Count; i++)
        {
            if (_equations[i] is IDragConstraint dc)
            {
                if (!checkDrag) continue;
                if (dc.IsDrag) continue;
            }
            if (Math.Abs(_b![i]) >= Epsilon)
                return false;
        }
        return true;
    }

    /// <summary>Get the maximum parameter change from the last iteration</summary>
    public double GetMaxParamChange()
    {
        double result = 0.0;
        for (int i = 0; i < _parameters.Count; i++)
        {
            result = Math.Max(Math.Abs(_parameters[i].value - _oldParamValues![i]), result);
        }
        return result;
    }

    private void StoreParams()
    {
        for (int i = 0; i < _parameters.Count; i++)
        {
            _oldParamValues![i] = _parameters[i].value;
        }
    }

    private void RevertParams()
    {
        for (int i = 0; i < _parameters.Count; i++)
        {
            _parameters[i].value = _oldParamValues![i];
        }
    }

    private void PerturbParams(double range)
    {
        var random = new Random();
        for (int i = 0; i < _parameters.Count; i++)
        {
            _parameters[i].value += (random.NextDouble() * 2.0 - 1.0) * range;
        }
    }

    private Exp[,] WriteJacobian(List<Exp> equations, List<Param> parameters)
    {
        var depends = equations.Select(eq => eq.DependOnParams()).ToList();

        int cols = parameters.Count;
        _nzColumns = new List<int>[cols];
        for (int c = 0; c < cols; c++)
            _nzColumns[c] = new List<int>();

        _nzRows = new List<int>[equations.Count];
        for (int r = 0; r < equations.Count; r++)
            _nzRows[r] = new List<int>();

        var j = new Exp[equations.Count, parameters.Count];
        for (int r = 0; r < equations.Count; r++)
        {
            var eq = equations[r];
            var depend = depends[r];
            for (int c = 0; c < parameters.Count; c++)
            {
                var u = parameters[c];
                if (!depend.Contains(u))
                {
                    j[r, c] = Exp.zero;
                    continue;
                }
                j[r, c] = eq.Deriv(u);
                _nzColumns[c].Add(r);
                _nzRows[r].Add(c);
            }
        }
        return j;
    }

    /// <summary>Evaluate the Jacobian matrix numerically</summary>
    public void EvalJacobian(Exp[,] j, ref double[,] a, bool clearDrag)
    {
        UpdateDirty();
        int rows = j.GetLength(0);
        int cols = j.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
                a[r, c] = 0.0;

            if (clearDrag && _equations[r] is IDragConstraint dc && dc.IsDrag)
                continue;

            foreach (int c in _nzRows![r])
            {
                var v = j[r, c].Eval();
                if (double.IsNaN(v))
                    v = 1.0;
                a[r, c] = v;
            }
        }
    }

    /// <summary>Compute A^T * A for least squares</summary>
    public void MakeAAT(double[,] a, double[,] aat)
    {
        int rows = a.GetLength(0);
        int cols = a.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < rows; c++)
                aat[r, c] = 0.0;
        }

        for (int i = 0; i < cols; i++)
        {
            var nzColumn = _nzColumns![i];
            foreach (int r in nzColumn)
            {
                foreach (int c in nzColumn)
                {
                    aat[r, c] += a[r, i] * a[c, i];
                }
            }
        }
    }

    /// <summary>Solve using least squares method</summary>
    public void SolveLeastSquares(double[,] a, double[] b, ref double[] x)
    {
        MakeAAT(a, _aat!);
        GaussianMethod.Solve(_aat!, b, ref _z!);
        int rows = a.GetLength(0);
        int cols = a.GetLength(1);

        for (int c = 0; c < cols; c++)
        {
            double sum = 0.0;
            for (int r = 0; r < rows; r++)
                sum += _z![r] * a[r, c];
            x[c] = sum;
        }
    }

    /// <summary>Clear all equations and parameters</summary>
    public void Clear()
    {
        _parameters.Clear();
        _currentParams.Clear();
        _equations.Clear();
        _sourceEquations.Clear();
        IsDirty = true;
    }

    /// <summary>Test the rank of the Jacobian</summary>
    public bool TestRank(out int dof)
    {
        EvalJacobian(_jacobian!, ref _a!, clearDrag: false);
        int rank = GaussianMethod.Rank(_a!);
        dof = _a!.GetLength(1) - rank;
        return rank == _a.GetLength(0);
    }

    private void UpdateDirty()
    {
        if (!IsDirty) return;

        _equations = _sourceEquations.Select(e => e.DeepClone()).ToList();
        _currentParams = _parameters.ToList();

        // Solve by substitution
        var subs = SolveBySubstitution();

        // Write Jacobian
        _jacobian = WriteJacobian(_equations, _currentParams);

        // Allocate arrays
        _a = new double[_jacobian.GetLength(0), _jacobian.GetLength(1)];
        _b = new double[_equations.Count];
        _x = new double[_currentParams.Count];
        _z = new double[_jacobian.GetLength(0)];
        _aat = new double[_jacobian.GetLength(0), _jacobian.GetLength(0)];
        _oldParamValues = new double[_parameters.Count];

        IsDirty = false;
    }

    private void BackSubstitution(Dictionary<Param, Param> subs)
    {
        if (subs == null) return;
        for (int i = 0; i < _parameters.Count; i++)
        {
            var p = _parameters[i];
            if (!subs.TryGetValue(p, out var replacement)) continue;
            p.value = replacement.value;
        }
    }

    private Dictionary<Param, Param> SolveBySubstitution()
    {
        var subs = new Dictionary<Param, Param>();
        var newParams = new HashSet<Param>(_currentParams);

        Param GetLastSubstitution(Param p)
        {
            Param current = p;
            while (subs.ContainsKey(current))
            {
                current = subs[current];
                if (current == p)
                {
                    subs.Remove(current);
                    break;
                }
            }
            return current;
        }

        for (int i = 0; i < _equations.Count; i++)
        {
            var eq = _equations[i];
            if (eq is not EqExp substForm) continue;

            var a = substForm.a;
            var b = substForm.b;

            if (a == b)
            {
                _equations.RemoveAt(i--);
                continue;
            }

            if (Math.Abs(a.value - b.value) > Epsilon) continue;

            if (!newParams.Contains(b))
            {
                (a, b) = (b, a);
            }

            if (!newParams.Contains(b)) continue;

            Param last = GetLastSubstitution(b);
            subs[last] = a;

            if (subs.ContainsKey(a))
            {
                GetLastSubstitution(a);
            }

            _equations.RemoveAt(i--);
            newParams.Remove(b);
        }

        _currentParams = newParams.ToList();

        var backSubs = new Dictionary<Param, Param>();
        foreach (var p in subs.Keys)
        {
            var last = GetLastSubstitution(p);
            if (last == p) continue;
            backSubs[p] = last;
        }

        for (int i = 0; i < _equations.Count; i++)
        {
            var eq = _equations[i];
            var depends = eq.DependOnParams();
            foreach (var p in depends)
            {
                if (backSubs.TryGetValue(p, out var replacement))
                {
                    _equations[i] = eq.Substitute(p, replacement);
                    eq = _equations[i];
                }
            }
        }

        return subs;
    }

    /// <summary>Solve the constraint system</summary>
    public SolveResult Solve()
    {
        UpdateDirty();
        StoreParams();

        bool clearDrag = true;
        int steps = HasDragged() ? DragSteps : MaxSteps;

        for (int attempt = 0; attempt <= PerturbationSteps; attempt++)
        {
            for (int step = 0; step < steps; step++)
            {
                EvalJacobian(_jacobian!, ref _a!, clearDrag);
                Eval(ref _b!, clearDrag);

                if (IsConverged(checkDrag: false))
                    return SolveResult.OKAY;

                if (AvoidJumping)
                {
                    for (int i = 0; i < _currentParams.Count; i++)
                    {
                        _x[i] = 0.0;
                    }

                    SolveLeastSquares(_a!, _b!, ref _x!);

                    double maxChange = 0.0;
                    for (int i = 0; i < _currentParams.Count; i++)
                    {
                        var change = Math.Abs(_x![i]);
                        if (change > maxChange)
                            maxChange = change;
                    }

                    if (maxChange > JumpFactor)
                    {
                        return SolveResult.JUMP;
                    }
                }

                // Newton step: delta = -J^(-1) * F
                for (int i = 0; i < _currentParams.Count; i++)
                {
                    _x![i] = -_x![i];
                }

                // Apply parameter updates
                for (int i = 0; i < _currentParams.Count; i++)
                {
                    _currentParams[i].value += _x![i];
                }

                // Check for NaN
                for (int i = 0; i < _currentParams.Count; i++)
                {
                    if (double.IsNaN(_currentParams[i].value))
                    {
                        RevertParams();
                        return SolveResult.INTERNAL_FAILURE;
                    }
                }
            }

            if (attempt < PerturbationSteps)
            {
                PerturbParams(0.01);
                clearDrag = false;
            }
        }

        if (RevertWhenNotConverged)
            RevertParams();

        return HasDragged() ? SolveResult.OKAY : SolveResult.DIDNT_CONVEGE;
    }

    private static bool IsZero(Exp exp)
    {
        return exp is ConstExp c && Math.Abs(c.Eval()) < Epsilon;
    }

    /// <summary>Mark the system as needing rebuild</summary>
    public void MarkDirty()
    {
        IsDirty = true;
    }
}

/// <summary>Interface for drag constraints</summary>
public interface IDragConstraint
{
    bool IsDrag { get; set; }
}
