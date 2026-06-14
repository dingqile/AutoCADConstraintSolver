using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AutoCADConstraintSolver.Converter;
using AutoCADConstraintSolver.Constraints;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Sketch;

namespace AutoCADConstraintSolver;

/// <summary>
/// AutoCAD commands for the constraint solver
/// </summary>
public class Commands
{
    private static SketchDocument? _currentDocument;

    /// <summary>
    /// Main solve command - solve all constraints in the sketch
    /// </summary>
    [CommandMethod("CSSOLVE")]
    public static void SolveCommand()
    {
        var doc = GetOrCreateDocument();
        var result = doc.Sketch.Solve();

        if (result == Solver.SolveResult.OKAY)
        {
            doc.SyncToAutoCAD();
            WriteMessage("Constraints solved successfully.");
        }
        else
        {
            WriteMessage($"Solver failed: {result}");
        }
    }

    /// <summary>
    /// Add a line from two picked points
    /// </summary>
    [CommandMethod("CSADDLINE")]
    public static void AddLineCommand()
    {
        var doc = GetOrCreateDocument();

        var p1 = GetPoint("\nPick first point: ");
        if (p1 == null) return;

        var p2 = GetPoint("\nPick second point: ");
        if (p2 == null) return;

        var line = new LineEntity(p1.Value, p2.Value);
        doc.Sketch.AddEntity(line);

        WriteMessage($"Line added: {line}");
    }

    /// <summary>
    /// Add a circle from center and radius
    /// </summary>
    [CommandMethod("CSADDCIRCLE")]
    public static void AddCircleCommand()
    {
        var doc = GetOrCreateDocument();

        var center = GetPoint("\nPick circle center: ");
        if (center == null) return;

        var options = new PromptDistanceOptions("\nEnter circle radius: ");
        options.DefaultValue = 50;
        options.UseDefaultValue = true;

        var result = GetEditor().GetDistance(options);
        if (result.Status != PromptStatus.OK) return;

        var circle = new CircleEntity(center.Value, result.Value);
        doc.Sketch.AddEntity(circle);

        WriteMessage($"Circle added: {circle}");
    }

    /// <summary>
    /// Add an arc from center, radius, start and end angles
    /// </summary>
    [CommandMethod("CSADDARC")]
    public static void AddArcCommand()
    {
        var doc = GetOrCreateDocument();

        var center = GetPoint("\nPick arc center: ");
        if (center == null) return;

        var radiusResult = GetNumber("\nEnter radius: ", 50);
        if (!radiusResult.HasValue) return;

        var startResult = GetNumber("\nEnter start angle (degrees): ", 0);
        if (!startResult.HasValue) return;

        var endResult = GetNumber("\nEnter end angle (degrees): ", 90);
        if (!endResult.HasValue) return;

        var arc = new ArcEntity(
            center.Value,
            radiusResult.Value,
            startResult.Value * Math.PI / 180,
            endResult.Value * Math.PI / 180);

        doc.Sketch.AddEntity(arc);

        WriteMessage($"Arc added: {arc}");
    }

    /// <summary>
    /// Add horizontal constraint to selected line
    /// </summary>
    [CommandMethod("CSHORIZONTAL")]
    public static void HorizontalConstraintCommand()
    {
        var doc = GetOrCreateDocument();
        var line = SelectEntity<LineEntity>("\nSelect a line: ");
        if (line == null) return;

        var constraint = new HorizontalConstraint(line);
        doc.Sketch.AddConstraint(constraint);
        doc.Sketch.Solve();
        doc.SyncToAutoCAD();

        WriteMessage("Horizontal constraint added.");
    }

    /// <summary>
    /// Add vertical constraint to selected line
    /// </summary>
    [CommandMethod("CSVERTICAL")]
    public static void VerticalConstraintCommand()
    {
        var doc = GetOrCreateDocument();
        var line = SelectEntity<LineEntity>("\nSelect a line: ");
        if (line == null) return;

        var constraint = new VerticalConstraint(line);
        doc.Sketch.AddConstraint(constraint);
        doc.Sketch.Solve();
        doc.SyncToAutoCAD();

        WriteMessage("Vertical constraint added.");
    }

    /// <summary>
    /// Add parallel constraint to two selected lines
    /// </summary>
    [CommandMethod("CSPARALLEL")]
    public static void ParallelConstraintCommand()
    {
        var doc = GetOrCreateDocument();

        var line1 = SelectEntity<LineEntity>("\nSelect first line: ");
        if (line1 == null) return;

        var line2 = SelectEntity<LineEntity>("\nSelect second line: ");
        if (line2 == null) return;

        var constraint = new ParallelConstraint(line1, line2);
        doc.Sketch.AddConstraint(constraint);
        doc.Sketch.Solve();
        doc.SyncToAutoCAD();

        WriteMessage("Parallel constraint added.");
    }

    /// <summary>
    /// Add perpendicular constraint to two selected lines
    /// </summary>
    [CommandMethod("CSPERPENDICULAR")]
    public static void PerpendicularConstraintCommand()
    {
        var doc = GetOrCreateDocument();

        var line1 = SelectEntity<LineEntity>("\nSelect first line: ");
        if (line1 == null) return;

        var line2 = SelectEntity<LineEntity>("\nSelect second line: ");
        if (line2 == null) return;

        var constraint = new PerpendicularConstraint(line1, line2);
        doc.Sketch.AddConstraint(constraint);
        doc.Sketch.Solve();
        doc.SyncToAutoCAD();

        WriteMessage("Perpendicular constraint added.");
    }

    /// <summary>
    /// Add distance constraint between two points
    /// </summary>
    [CommandMethod("CSDISTANCE")]
    public static void DistanceConstraintCommand()
    {
        var doc = GetOrCreateDocument();

        var p1 = SelectEntity<PointEntity>("\nSelect first point: ");
        var p2 = SelectEntity<PointEntity>("\nSelect second point: ");

        if (p1 == null || p2 == null)
        {
            // Try selecting lines and getting their endpoints
            var line1 = SelectEntity<LineEntity>("\nSelect first line: ");
            var line2 = SelectEntity<LineEntity>("\nSelect second line: ");

            if (line1 == null || line2 == null)
            {
                WriteMessage("Please select two lines or points.");
                return;
            }

            p1 = line1.Start;
            p2 = line2.Start;
        }

        var distResult = GetNumber("\nEnter distance: ", 100);
        if (!distResult.HasValue) return;

        var constraint = new PointsDistanceConstraint(p1, p2, distResult.Value);
        doc.Sketch.AddConstraint(constraint);
        doc.Sketch.Solve();
        doc.SyncToAutoCAD();

        WriteMessage($"Distance constraint ({distResult.Value}) added.");
    }

    /// <summary>
    /// Add coincident constraint between two points
    /// </summary>
    [CommandMethod("CSCOINCIDENT")]
    public static void CoincidentConstraintCommand()
    {
        var doc = GetOrCreateDocument();

        var p1 = SelectEntity<PointEntity>("\nSelect first point: ");
        var p2 = SelectEntity<PointEntity>("\nSelect second point: ");

        if (p1 == null || p2 == null)
        {
            WriteMessage("Please select two points.");
            return;
        }

        var constraint = new CoincidentConstraint(p1, p2);
        doc.Sketch.AddConstraint(constraint);
        doc.Sketch.Solve();
        doc.SyncToAutoCAD();

        WriteMessage("Coincident constraint added.");
    }

    /// <summary>
    /// Add tangent constraint between a line and circle/arc
    /// </summary>
    [CommandMethod("CSTANGENT")]
    public static void TangentConstraintCommand()
    {
        var doc = GetOrCreateDocument();

        var line = SelectEntity<LineEntity>("\nSelect a line: ");
        if (line == null) return;

        CircleEntity? circle = null;
        ArcEntity? arc = null;

        var circ = SelectEntity<CircleEntity>("\nSelect a circle: ");
        if (circ != null)
            circle = circ;
        else
            arc = SelectEntity<ArcEntity>("\nSelect an arc: ");

        Constraint? constraint = null;
        if (circle != null)
            constraint = new TangentConstraint(line, circle);
        else if (arc != null)
            constraint = new TangentConstraint(line, arc);

        if (constraint == null)
        {
            WriteMessage("Please select a line and a circle or arc.");
            return;
        }

        doc.Sketch.AddConstraint(constraint);
        doc.Sketch.Solve();
        doc.SyncToAutoCAD();

        WriteMessage("Tangent constraint added.");
    }

    /// <summary>
    /// Fix a point at a specific location
    /// </summary>
    [CommandMethod("CSFIX")]
    public static void FixConstraintCommand()
    {
        var doc = GetOrCreateDocument();

        var point = SelectEntity<PointEntity>("\nSelect a point to fix: ");
        if (point == null)
        {
            // Try selecting a line and getting its endpoint
            var line = SelectEntity<LineEntity>("\nSelect a line: ");
            if (line != null)
            {
                point = line.Start;
            }
        }

        if (point == null)
        {
            WriteMessage("Please select a point or line.");
            return;
        }

        var constraint = new FixationConstraint(point, point.Position);
        doc.Sketch.AddConstraint(constraint);
        doc.Sketch.Solve();
        doc.SyncToAutoCAD();

        WriteMessage($"Point fixed at ({point.Position.X:F2}, {point.Position.Y:F2}).");
    }

    /// <summary>
    /// Import entities from AutoCAD to the constraint solver
    /// </summary>
    [CommandMethod("CSIMPORT")]
    public static void ImportCommand()
    {
        var doc = GetOrCreateDocument();
        doc.ImportFromAutoCAD();
        WriteMessage($"Imported {doc.Sketch.Entities.Count} entities from AutoCAD.");
    }

    /// <summary>
    /// Export entities back to AutoCAD
    /// </summary>
    [CommandMethod("CSEXPORT")]
    public static void ExportCommand()
    {
        var doc = GetOrCreateDocument();
        doc.SyncToAutoCAD();
        WriteMessage($"Exported {doc.Sketch.Entities.Count} entities to AutoCAD.");
    }

    /// <summary>
    /// Show sketch statistics
    /// </summary>
    [CommandMethod("CSSTATS")]
    public static void StatsCommand()
    {
        var doc = GetOrCreateDocument();
        var sketch = doc.Sketch;

        WriteMessage("=== Sketch Statistics ===");
        WriteMessage($"Entities: {sketch.Entities.Count}");
        WriteMessage($"Constraints: {sketch.Constraints.Count}");
        WriteMessage($"DOF: {sketch.GetDegreesOfFreedom()}");

        var lines = sketch.Entities.OfType<LineEntity>().Count();
        var circles = sketch.Entities.OfType<CircleEntity>().Count();
        var arcs = sketch.Entities.OfType<ArcEntity>().Count();

        WriteMessage($"  Lines: {lines}");
        WriteMessage($"  Circles: {circles}");
        WriteMessage($"  Arcs: {arcs}");
    }

    /// <summary>
    /// Clear all entities and constraints
    /// </summary>
    [CommandMethod("CSCLEAR")]
    public static void ClearCommand()
    {
        var doc = GetOrCreateDocument();
        doc.Sketch.Clear();
        _currentDocument = null;
        WriteMessage("Sketch cleared.");
    }

    #region Helper Methods

    private static SketchDocument GetOrCreateDocument()
    {
        _currentDocument ??= new SketchDocument();
        return _currentDocument;
    }

    private static Vec2? GetPoint(string prompt)
    {
        var options = new PromptPointOptions(prompt);
        var result = GetEditor().GetPoint(options);

        if (result.Status != PromptStatus.OK)
            return null;

        return new Vec2(result.Value.X, result.Value.Y);
    }

    private static double? GetNumber(string prompt, double defaultValue)
    {
        var options = new PromptDoubleOptions(prompt)
        {
            DefaultValue = defaultValue,
            UseDefaultValue = true
        };

        var result = GetEditor().GetDouble(options);
        return result.Status == PromptStatus.OK ? result.Value : null;
    }

    private static T? SelectEntity<T>(string prompt) where T : Entity
    {
        var options = new PromptEntityOptions(prompt);
        var result = GetEditor().GetEntity(options);

        if (result.Status != PromptStatus.OK)
            return null;

        // This is a placeholder - actual implementation would use the converter
        return null;
    }

    private static Editor GetEditor()
    {
        return Application.DocumentManager.MdiActiveDocument!.Editor;
    }

    private static void WriteMessage(string message)
    {
        GetEditor().WriteMessage($"\n{message}");
    }

    #endregion
}