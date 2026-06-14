using System;
using System.Collections.Generic;
using AutoCADConstraintSolver.Constraints;
using AutoCADConstraintSolver.Entities;

namespace AutoCADConstraintSolver.Sketch;

/// <summary>
/// Represents a single action that can be undone/redone
/// </summary>
public interface ISketchAction
{
    string Description { get; }
    void Execute();
    void Undo();
}

/// <summary>
/// Add entity action
/// </summary>
public class AddEntityAction : ISketchAction
{
    private readonly Sketch _sketch;
    private readonly Entity _entity;

    public string Description => $"Add {_entity.GetType().Name}";

    public AddEntityAction(Sketch sketch, Entity entity)
    {
        _sketch = sketch;
        _entity = entity;
    }

    public void Execute()
    {
        if (!_sketch.Entities.Contains(_entity))
            _sketch.AddEntity(_entity);
    }

    public void Undo()
    {
        _sketch.RemoveEntity(_entity);
    }
}

/// <summary>
/// Remove entity action
/// </summary>
public class RemoveEntityAction : ISketchAction
{
    private readonly Sketch _sketch;
    private readonly Entity _entity;
    private readonly List<Constraint> _removedConstraints;

    public string Description => $"Remove {_entity.GetType().Name}";

    public RemoveEntityAction(Sketch sketch, Entity entity)
    {
        _sketch = sketch;
        _entity = entity;
        _removedConstraints = new List<Constraint>(sketch.GetConstraintsFor(_entity));
    }

    public void Execute()
    {
        _sketch.RemoveEntity(_entity);
    }

    public void Undo()
    {
        if (!_sketch.Entities.Contains(_entity))
        {
            _sketch.AddEntity(_entity);
            foreach (var constraint in _removedConstraints)
            {
                if (!_sketch.Constraints.Contains(constraint))
                    _sketch.AddConstraint(constraint);
            }
        }
    }
}

/// <summary>
/// Add constraint action
/// </summary>
public class AddConstraintAction : ISketchAction
{
    private readonly Sketch _sketch;
    private readonly Constraint _constraint;

    public string Description => $"Add {_constraint.DisplayName}";

    public AddConstraintAction(Sketch sketch, Constraint constraint)
    {
        _sketch = sketch;
        _constraint = constraint;
    }

    public void Execute()
    {
        if (!_sketch.Constraints.Contains(_constraint))
            _sketch.AddConstraint(_constraint);
    }

    public void Undo()
    {
        _sketch.RemoveConstraint(_constraint);
    }
}

/// <summary>
/// Remove constraint action
/// </summary>
public class RemoveConstraintAction : ISketchAction
{
    private readonly Sketch _sketch;
    private readonly Constraint _constraint;

    public string Description => $"Remove {_constraint.DisplayName}";

    public RemoveConstraintAction(Sketch sketch, Constraint constraint)
    {
        _sketch = sketch;
        _constraint = constraint;
    }

    public void Execute()
    {
        _sketch.RemoveConstraint(_constraint);
    }

    public void Undo()
    {
        if (!_sketch.Constraints.Contains(_constraint))
            _sketch.AddConstraint(_constraint);
    }
}

/// <summary>
/// Move entity action
/// </summary>
public class MoveEntityAction : ISketchAction
{
    private readonly Sketch _sketch;
    private readonly Entity _entity;
    private readonly Geometry.Vec2 _delta;

    public string Description => $"Move {_entity.GetType().Name}";

    public MoveEntityAction(Sketch sketch, Entity entity, Geometry.Vec2 delta)
    {
        _sketch = sketch;
        _entity = entity;
        _delta = delta;
    }

    public void Execute()
    {
        _entity.Move(_delta);
        _sketch.MarkDirty();
    }

    public void Undo()
    {
        _entity.Move(-_delta);
        _sketch.MarkDirty();
    }
}

/// <summary>
/// Solve action
/// </summary>
public class SolveAction : ISketchAction
{
    private readonly Sketch _sketch;
    private readonly Dictionary<Entity, Entity> _snapshot;

    public string Description => "Solve Constraints";

    public SolveAction(Sketch sketch)
    {
        _sketch = sketch;
        _snapshot = new Dictionary<Entity, Entity>();
        
        foreach (var entity in sketch.Entities)
        {
            _snapshot[entity] = entity.Clone();
        }
    }

    public void Execute()
    {
        _sketch.Solve();
    }

    public void Undo()
    {
        foreach (var (entity, snapshot) in _snapshot)
        {
            RestoreEntity(entity, snapshot);
        }
        _sketch.MarkDirty();
    }

    private void RestoreEntity(Entity entity, Entity snapshot)
    {
        switch (entity)
        {
            case PointEntity p:
                if (snapshot is PointEntity sp)
                {
                    p.Position = sp.Position;
                }
                break;
            case LineEntity l:
                if (snapshot is LineEntity sl)
                {
                    l.StartPosition = sl.StartPosition;
                    l.EndPosition = sl.EndPosition;
                }
                break;
            case CircleEntity c:
                if (snapshot is CircleEntity sc)
                {
                    c.CenterPosition = sc.CenterPosition;
                    c.RadiusValue = sc.RadiusValue;
                }
                break;
            case ArcEntity a:
                if (snapshot is ArcEntity sa)
                {
                    a.CenterPosition = sa.CenterPosition;
                    a.RadiusValue = sa.RadiusValue;
                    a.StartAngleValue = sa.StartAngleValue;
                    a.EndAngleValue = sa.EndAngleValue;
                }
                break;
        }
    }
}

/// <summary>
/// Command history for undo/redo operations
/// </summary>
public class CommandHistory
{
    private readonly List<ISketchAction> _undoStack = new();
    private readonly List<ISketchAction> _redoStack = new();
    private readonly int _maxHistorySize;
    private readonly Action<string>? _onDescriptionChanged;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    
    public string UndoDescription => _undoStack.Count > 0 
        ? _undoStack[^1].Description 
        : string.Empty;
    
    public string RedoDescription => _redoStack.Count > 0 
        ? _redoStack[^1].Description 
        : string.Empty;

    public CommandHistory(int maxHistorySize = 100, Action<string>? onDescriptionChanged = null)
    {
        _maxHistorySize = maxHistorySize;
        _onDescriptionChanged = onDescriptionChanged;
    }

    /// <summary>
    /// Execute and record an action
    /// </summary>
    public void Execute(ISketchAction action)
    {
        action.Execute();
        _undoStack.Add(action);
        _redoStack.Clear();
        
        TrimHistory();
        _onDescriptionChanged?.Invoke($"Undo: {UndoDescription}");
    }

    /// <summary>
    /// Undo the last action
    /// </summary>
    public bool Undo()
    {
        if (!CanUndo) return false;

        var action = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        action.Undo();
        _redoStack.Add(action);
        
        _onDescriptionChanged?.Invoke(CanUndo ? $"Undo: {UndoDescription}" : "Nothing to undo");
        return true;
    }

    /// <summary>
    /// Redo the last undone action
    /// </summary>
    public bool Redo()
    {
        if (!CanRedo) return false;

        var action = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        action.Execute();
        _undoStack.Add(action);
        
        _onDescriptionChanged?.Invoke($"Undo: {UndoDescription}");
        return true;
    }

    /// <summary>
    /// Clear all history
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _onDescriptionChanged?.Invoke("History cleared");
    }

    private void TrimHistory()
    {
        while (_undoStack.Count > _maxHistorySize)
        {
            _undoStack.RemoveAt(0);
        }
    }
}
