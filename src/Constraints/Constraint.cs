using System;
using System.Collections.Generic;
using AutoCADConstraintSolver.Solver;

namespace AutoCADConstraintSolver.Constraints;

/// <summary>
/// Base class for all geometric constraints
/// </summary>
public abstract class Constraint
{
    /// <summary>Unique identifier for this constraint</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Display name of the constraint type</summary>
    public abstract string DisplayName { get; }

    /// <summary>Whether this constraint is fully defined</summary>
    public virtual bool IsValid => true;

    /// <summary>
    /// Get the entities involved in this constraint
    /// </summary>
    public abstract IEnumerable<Entity> GetEntities();

    /// <summary>
    /// Setup equations for this constraint in the solver
    /// </summary>
    public abstract void SetupEquations(EquationSystem system);

    /// <summary>
    /// Remove equations for this constraint from the solver
    /// </summary>
    public abstract void RemoveEquations(EquationSystem system);

    /// <summary>
    /// Get the degrees of freedom consumed by this constraint
    /// </summary>
    public virtual int DegreesOfFreedom => 1;

    /// <summary>
    /// Get constraint value for driving constraints
    /// </summary>
    public virtual double Value { get; set; }

    /// <summary>
    /// Get the driving status (for dimensional constraints)
    /// </summary>
    public virtual bool IsDriving => true;
}

/// <summary>
/// Constraint involving a single entity
/// </summary>
public abstract class SingleEntityConstraint : Constraint
{
    protected Entity entity;

    public SingleEntityConstraint(Entity entity)
    {
        this.entity = entity;
    }

    public override IEnumerable<Entity> GetEntities() => new[] { entity };
}

/// <summary>
/// Constraint involving two entities
/// </summary>
public abstract class TwoEntityConstraint : Constraint
{
    protected Entity entity1;
    protected Entity entity2;

    public TwoEntityConstraint(Entity entity1, Entity entity2)
    {
        this.entity1 = entity1;
        this.entity2 = entity2;
    }

    public override IEnumerable<Entity> GetEntities() => new[] { entity1, entity2 };
}

/// <summary>
/// Constraint involving three entities
/// </summary>
public abstract class ThreeEntityConstraint : Constraint
{
    protected Entity entity1;
    protected Entity entity2;
    protected Entity entity3;

    public ThreeEntityConstraint(Entity entity1, Entity entity2, Entity entity3)
    {
        this.entity1 = entity1;
        this.entity2 = entity2;
        this.entity3 = entity3;
    }

    public override IEnumerable<Entity> GetEntities() => new[] { entity1, entity2, entity3 };
}

/// <summary>
/// Dimensional constraint with a value
/// </summary>
public abstract class DimensionalConstraint : Constraint
{
    public double Value { get; set; }
    public bool IsDriving { get; set; } = true;
    
    public override bool IsDriving1 => IsDriving;
    
    public override double Value1 
    { 
        get => Value; 
        set => Value = value; 
    }
}
