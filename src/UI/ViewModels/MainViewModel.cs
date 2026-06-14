using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AutoCADConstraintSolver.Constraints;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Sketch;
using AutoCADConstraintSolver.Solver;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoCADConstraintSolver.UI.ViewModels;

/// <summary>
/// Main view model for the constraint solver UI
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly Sketch _sketch;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isSolved;

    [ObservableProperty]
    private string _degreesOfFreedom = "0";

    [ObservableProperty]
    private Entity? _selectedEntity;

    [ObservableProperty]
    private Constraint? _selectedConstraint;

    [ObservableProperty]
    private double _sketchWidth = 800;

    [ObservableProperty]
    private double _sketchHeight = 600;

    public ObservableCollection<Entity> Entities { get; } = new();
    public ObservableCollection<Constraint> Constraints { get; } = new();

    public Sketch Sketch => _sketch;

    public MainViewModel()
    {
        _sketch = new Sketch();
        _sketch.Modified += OnSketchModified;
        _sketch.SolveCompleted += OnSolveCompleted;
    }

    private void OnSketchModified()
    {
        UpdateDegreesOfFreedom();
        StatusMessage = "Modified";
    }

    private void OnSolveCompleted(SolveResult result)
    {
        IsSolved = result == SolveResult.OKAY;
        StatusMessage = result switch
        {
            SolveResult.OKAY => "Solved successfully",
            SolveResult.DIDNT_CONVEGE => "Warning: Did not converge",
            SolveResult.REDUNDANT => "Warning: Redundant constraints",
            SolveResult.POSTPONE => "Postponed",
            SolveResult.INTERNAL_FAILURE => "Error: Internal failure",
            SolveResult.JUMP => "Warning: Solution jumped",
            _ => "Unknown"
        };
    }

    private void UpdateDegreesOfFreedom()
    {
        var dof = _sketch.GetDegreesOfFreedom();
        DegreesOfFreedom = dof >= 0 ? dof.ToString() : $"Under-constrained by {-dof}";
    }

    [RelayCommand]
    private void AddLine()
    {
        var line = new LineEntity(new Vec2(100, 100), new Vec2(300, 100));
        _sketch.AddEntity(line);
        Entities.Add(line);
        UpdateCollections();
    }

    [RelayCommand]
    private void AddCircle()
    {
        var circle = new CircleEntity(new Vec2(200, 200), 50);
        _sketch.AddEntity(circle);
        Entities.Add(circle);
        UpdateCollections();
    }

    [RelayCommand]
    private void AddArc()
    {
        var arc = new ArcEntity(new Vec2(200, 200), 50, 0, Math.PI / 2);
        _sketch.AddEntity(arc);
        Entities.Add(arc);
        UpdateCollections();
    }

    [RelayCommand]
    private void Solve()
    {
        _sketch.Solve();
        UpdateCollections();
    }

    [RelayCommand]
    private void Clear()
    {
        _sketch.Clear();
        Entities.Clear();
        Constraints.Clear();
        UpdateDegreesOfFreedom();
        StatusMessage = "Cleared";
    }

    public void AddConstraint(string constraintType, Entity entity1, Entity? entity2 = null)
    {
        Constraint? constraint = constraintType switch
        {
            "Horizontal" when entity1 is LineEntity line => new HorizontalConstraint(line),
            "Vertical" when entity1 is LineEntity line => new VerticalConstraint(line),
            "Parallel" when entity1 is LineEntity l1 && entity2 is LineEntity l2 => new ParallelConstraint(l1, l2),
            "Perpendicular" when entity1 is LineEntity l1 && entity2 is LineEntity l2 => new PerpendicularConstraint(l1, l2),
            "Tangent" when entity1 is LineEntity line && entity2 is CircleEntity circle => new TangentConstraint(line, circle),
            "Tangent" when entity1 is LineEntity line && entity2 is ArcEntity arc => new TangentConstraint(line, arc),
            "EqualLength" when entity1 is LineEntity l1 && entity2 is LineEntity l2 => new EqualLengthConstraint(l1, l2),
            "Distance" when entity1 is PointEntity p1 && entity2 is PointEntity p2 => new PointsDistanceConstraint(p1, p2, 100),
            "Length" when entity1 is LineEntity line => new LengthConstraint(line, 100),
            "Diameter" when entity1 is CircleEntity circle => new DiameterConstraint(circle, 50),
            _ => null
        };

        if (constraint != null)
        {
            _sketch.AddConstraint(constraint);
            Constraints.Add(constraint);
            _sketch.Solve();
            UpdateCollections();
        }
    }

    public void SelectEntity(Entity entity)
    {
        _sketch.DeselectAll();
        entity.IsSelected = true;
        SelectedEntity = entity;
        UpdateCollections();
    }

    public void DeleteSelected()
    {
        if (SelectedEntity != null)
        {
            _sketch.RemoveEntity(SelectedEntity);
            UpdateCollections();
            SelectedEntity = null;
        }
    }

    private void UpdateCollections()
    {
        Entities.Clear();
        foreach (var e in _sketch.Entities)
            Entities.Add(e);

        Constraints.Clear();
        foreach (var c in _sketch.Constraints)
            Constraints.Add(c);

        UpdateDegreesOfFreedom();
    }

    public void CreateRectangle(double width, double height)
    {
        var rect = Sketch.CreateSimpleRectangle(width, height);
        
        // Copy entities to current sketch
        foreach (var entity in rect.Entities)
        {
            _sketch.AddEntity(entity);
        }
        foreach (var constraint in rect.Constraints)
        {
            _sketch.AddConstraint(constraint);
        }
        
        UpdateCollections();
        _sketch.Solve();
    }
}
