using System;
using System.Collections.Generic;
using System.Linq;
using AutoCADConstraintSolver.Constraints;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Sketch;

/// <summary>
/// Sketch model containing all entities and constraints
/// </summary>
public class Sketch
{
    private readonly List<Entity> _entities = new();
    private readonly List<Constraint> _constraints = new();
    private readonly EquationSystem _solver = new();

    /// <summary>All entities in the sketch</summary>
    public IReadOnlyList<Entity> Entities => _entities;

    /// <summary>All constraints in the sketch</summary>
    public IReadOnlyList<Constraint> Constraints => _constraints;

    /// <summary>The constraint solver</summary>
    public EquationSystem Solver => _solver;

    /// <summary>Event raised when the sketch is modified</summary>
    public event Action? Modified;

    /// <summary>Event raised when solving completes</summary>
    public event Action<SolveResult>? SolveCompleted;

    /// <summary>
    /// Add an entity to the sketch
    /// </summary>
    public void AddEntity(Entity entity)
    {
        _entities.Add(entity);
        entity.SetupEquations(_solver);
        _solver.MarkDirty();
        OnModified();
    }

    /// <summary>
    /// Remove an entity from the sketch
    /// </summary>
    public void RemoveEntity(Entity entity)
    {
        // Remove all constraints involving this entity
        var relatedConstraints = _constraints
            .Where(c => c.GetEntities().Contains(entity))
            .ToList();
        
        foreach (var constraint in relatedConstraints)
        {
            RemoveConstraint(constraint);
        }

        entity.RemoveEquations(_solver);
        _entities.Remove(entity);
        _solver.MarkDirty();
        OnModified();
    }

    /// <summary>
    /// Add a constraint to the sketch
    /// </summary>
    public void AddConstraint(Constraint constraint)
    {
        _constraints.Add(constraint);
        constraint.SetupEquations(_solver);
        _solver.MarkDirty();
        OnModified();
    }

    /// <summary>
    /// Remove a constraint from the sketch
    /// </summary>
    public void RemoveConstraint(Constraint constraint)
    {
        constraint.RemoveEquations(_solver);
        _constraints.Remove(constraint);
        _solver.MarkDirty();
        OnModified();
    }

    /// <summary>
    /// Get constraints involving an entity
    /// </summary>
    public IEnumerable<Constraint> GetConstraintsFor(Entity entity)
    {
        return _constraints.Where(c => c.GetEntities().Contains(entity));
    }

    /// <summary>
    /// Get selected entities
    /// </summary>
    public IEnumerable<Entity> GetSelectedEntities()
    {
        return _entities.Where(e => e.IsSelected);
    }

    /// <summary>
    /// Select all entities
    /// </summary>
    public void SelectAll()
    {
        foreach (var entity in _entities)
            entity.IsSelected = true;
    }

    /// <summary>
    /// Deselect all entities
    /// </summary>
    public void DeselectAll()
    {
        foreach (var entity in _entities)
            entity.IsSelected = false;
    }

    /// <summary>
    /// Solve the constraint system
    /// </summary>
    public SolveResult Solve()
    {
        var result = _solver.Solve();
        OnSolveCompleted(result);
        return result;
    }

    /// <summary>
    /// Try to solve and return whether successful
    /// </summary>
    public bool TrySolve(out SolveResult result)
    {
        result = Solve();
        return result == SolveResult.OKAY;
    }

    /// <summary>
    /// Get the degrees of freedom for the sketch
    /// </summary>
    public int GetDegreesOfFreedom()
    {
        var entityDOFs = _entities.Sum(e => GetEntityDOFs(e));
        var constraintDOFs = _constraints.Sum(c => c.DegreesOfFreedom);
        return entityDOFs - constraintDOFs;
    }

    private int GetEntityDOFs(Entity entity)
    {
        return entity switch
        {
            PointEntity => 2,
            LineEntity => 4,
            CircleEntity => 3,
            ArcEntity => 5,
            _ => 0
        };
    }

    /// <summary>
    /// Clear all entities and constraints
    /// </summary>
    public void Clear()
    {
        _solver.Clear();
        _constraints.Clear();
        _entities.Clear();
        OnModified();
    }

    /// <summary>
    /// Move selected entities by a delta
    /// </summary>
    public void MoveSelected(Vec2 delta)
    {
        foreach (var entity in GetSelectedEntities())
        {
            entity.Move(delta);
        }
        _solver.MarkDirty();
        OnModified();
    }

    /// <summary>
    /// Create a simple sketch with two constrained points
    /// </summary>
    public static Sketch CreateSimpleRectangle(double width, double height)
    {
        var sketch = new Sketch();

        // Create rectangle corners
        var p1 = new PointEntity(0, 0);
        var p2 = new PointEntity(width, 0);
        var p3 = new PointEntity(width, height);
        var p4 = new PointEntity(0, height);

        // Create lines
        var l1 = new LineEntity(p1, p2);
        var l2 = new LineEntity(p2, p3);
        var l3 = new LineEntity(p3, p4);
        var l4 = new LineEntity(p4, p1);

        // Add entities
        sketch.AddEntity(l1);
        sketch.AddEntity(l2);
        sketch.AddEntity(l3);
        sketch.AddEntity(l4);

        // Add constraints - make it a parallelogram
        sketch.AddConstraint(new ParallelConstraint(l1, l3)); // Top parallel to bottom
        sketch.AddConstraint(new ParallelConstraint(l2, l4)); // Left parallel to right
        
        // Add equal length constraints
        sketch.AddConstraint(new EqualLengthConstraint(l1, l2));
        
        // Fix one corner
        sketch.AddConstraint(new FixationConstraint(p1, new Vec2(0, 0)));

        return sketch;
    }

    protected virtual void OnModified()
    {
        Modified?.Invoke();
    }

    protected virtual void OnSolveCompleted(SolveResult result)
    {
        SolveCompleted?.Invoke(result);
    }
}
