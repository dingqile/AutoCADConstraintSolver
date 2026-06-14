using System;
using System.Linq;
using System.Windows;
using AutoCADConstraintSolver.Constraints;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Sketch;
using AutoCADConstraintSolver.Solver;
using AutoCADConstraintSolver.UI.ViewModels;

namespace AutoCADConstraintSolver.UI.Views;

/// <summary>
/// Main window for the constraint solver application
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SketchCanvas _canvas;

    public MainWindow()
    {
        InitializeComponent();
        
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
        _canvas = SketchCanvas;
        _canvas.Sketch.Modified += OnSketchModified;
        _canvas.Sketch.SolveCompleted += OnSolveCompleted;

        UpdateStatistics();
    }

    private void OnSketchModified()
    {
        UpdateStatistics();
    }

    private void OnSolveCompleted(SolveResult result)
    {
        Dispatcher.Invoke(() =>
        {
            SolverStatusText.Text = result == SolveResult.OKAY ? "Solved" : $"Error: {result}";
            UpdateStatistics();
        });
    }

    private void UpdateStatistics()
    {
        Dispatcher.Invoke(() =>
        {
            EntityCount.Text = _canvas.Sketch.Entities.Count.ToString();
            ConstraintCount.Text = _canvas.Sketch.Constraints.Count.ToString();
            DOFText.Text = _viewModel.DegreesOfFreedom;
            ConstraintsList.ItemsSource = _canvas.Sketch.Constraints.ToList();
        });
    }

    private void BtnLine_Click(object sender, RoutedEventArgs e)
    {
        _canvas.AddLine(new Vec2(100, 100), new Vec2(300, 100));
        UpdateStatistics();
    }

    private void BtnCircle_Click(object sender, RoutedEventArgs e)
    {
        _canvas.AddCircle(new Vec2(200, 200), 50);
        UpdateStatistics();
    }

    private void BtnArc_Click(object sender, RoutedEventArgs e)
    {
        var arc = new ArcEntity(new Vec2(200, 200), 50, 0, Math.PI / 2);
        _canvas.Sketch.AddEntity(arc);
        UpdateStatistics();
    }

    private void BtnHorizontal_Click(object sender, RoutedEventArgs e)
    {
        AddConstraintToSelected<LineEntity>("Horizontal", c => new HorizontalConstraint(c));
    }

    private void BtnVertical_Click(object sender, RoutedEventArgs e)
    {
        AddConstraintToSelected<LineEntity>("Vertical", c => new VerticalConstraint(c));
    }

    private void BtnParallel_Click(object sender, RoutedEventArgs e)
    {
        AddTwoEntityConstraint<LineEntity, LineEntity>("Parallel", 
            (e1, e2) => new ParallelConstraint(e1, e2));
    }

    private void BtnPerpendicular_Click(object sender, RoutedEventArgs e)
    {
        AddTwoEntityConstraint<LineEntity, LineEntity>("Perpendicular",
            (e1, e2) => new PerpendicularConstraint(e1, e2));
    }

    private void BtnCoincident_Click(object sender, RoutedEventArgs e)
    {
        AddTwoEntityConstraint<PointEntity, PointEntity>("Coincident",
            (e1, e2) => new CoincidentConstraint(e1, e2));
    }

    private void BtnDistance_Click(object sender, RoutedEventArgs e)
    {
        var selected = _canvas.Sketch.GetSelectedEntities().ToList();
        if (selected.Count == 2 && selected[0] is PointEntity p1 && selected[1] is PointEntity p2)
        {
            var dist = new PointsDistanceConstraint(p1, p2, 100);
            _canvas.Sketch.AddConstraint(dist);
            _canvas.Solve();
            UpdateStatistics();
        }
    }

    private void BtnTangent_Click(object sender, RoutedEventArgs e)
    {
        var selected = _canvas.Sketch.GetSelectedEntities().ToList();
        if (selected.Count == 2)
        {
            Constraint? constraint = null;
            
            if (selected[0] is LineEntity line1 && selected[1] is CircleEntity circle1)
            {
                constraint = new TangentConstraint(line1, circle1);
            }
            else if (selected[0] is LineEntity line2 && selected[1] is ArcEntity arc1)
            {
                constraint = new TangentConstraint(line2, arc1);
            }
            else if (selected[0] is CircleEntity circle2 && selected[1] is LineEntity line3)
            {
                constraint = new TangentConstraint(line3, circle2);
            }
            else if (selected[0] is ArcEntity arc2 && selected[1] is LineEntity line4)
            {
                constraint = new TangentConstraint(line4, arc2);
            }

            if (constraint != null)
            {
                _canvas.Sketch.AddConstraint(constraint);
                _canvas.Solve();
                UpdateStatistics();
            }
        }
    }

    private void BtnSolve_Click(object sender, RoutedEventArgs e)
    {
        _canvas.Solve();
        UpdateStatistics();
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        _canvas.Sketch.Clear();
        UpdateStatistics();
        StatusText.Text = "Cleared";
    }

    private void BtnRectangle_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CreateRectangle(200, 100);
        UpdateStatistics();
    }

    private void AddConstraintToSelected<T>(string name, Func<T, Constraint> factory) where T : Entity
    {
        var selected = _canvas.Sketch.GetSelectedEntities().OfType<T>().FirstOrDefault();
        if (selected != null)
        {
            var constraint = factory(selected);
            _canvas.Sketch.AddConstraint(constraint);
            _canvas.Solve();
            UpdateStatistics();
            StatusText.Text = $"{name} constraint added";
        }
        else
        {
            StatusText.Text = $"Select a {name} entity first";
        }
    }

    private void AddTwoEntityConstraint<T1, T2>(string name, Func<T1, T2, Constraint> factory) 
        where T1 : Entity where T2 : Entity
    {
        var selected = _canvas.Sketch.GetSelectedEntities().ToList();
        if (selected.Count >= 2 && selected[0] is T1 e1 && selected[1] is T2 e2)
        {
            var constraint = factory(e1, e2);
            _canvas.Sketch.AddConstraint(constraint);
            _canvas.Solve();
            UpdateStatistics();
            StatusText.Text = $"{name} constraint added";
        }
        else
        {
            StatusText.Text = $"Select two {name} entities";
        }
    }
}
