using System;
using System.Collections.Generic;
using System.Linq;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Constraints;

namespace AutoCADConstraintSolver.Tools;

/// <summary>
/// Tool for applying horizontal/vertical constraints
/// </summary>
public class HVTool : Tool
{
    private LineEntity? _selectedLine;

    public override string Name => "Horizontal/Vertical";
    public override string Shortcut => "H";

    public override void OnMouseMove(MouseEventArgs e)
    {
        _selectedLine = GetEntityAtPoint(e.Position) as LineEntity;
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var entity = GetEntityAtPoint(e.Position) as LineEntity;
        if (entity != null)
        {
            // Determine if horizontal or vertical based on angle
            var angle = Math.Abs(entity.Direction.Y) > Math.Abs(entity.Direction.X)
                ? "H" : "V";

            Constraint? constraint = angle == "H" 
                ? new HorizontalConstraint(entity) as Constraint
                : new VerticalConstraint(entity) as Constraint;

            if (constraint != null)
            {
                Sketch!.AddConstraint(constraint);
                Editor!.MarkModified();
            }
        }
    }

    public override void OnRender(RenderEventArgs e)
    {
        if (_selectedLine != null)
        {
            e.Renderer.DrawLine(
                _selectedLine.StartPosition,
                _selectedLine.EndPosition,
                0xFFFF00,
                3);
        }
    }
}

/// <summary>
/// Tool for adding parallel constraints
/// </summary>
public class ParallelTool : Tool
{
    private LineEntity? _firstLine;

    public override string Name => "Parallel";
    public override string Shortcut => "=";

    public override void OnMouseMove(MouseEventArgs e)
    {
        // Preview
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var line = GetEntityAtPoint(e.Position) as LineEntity;
        if (line != null)
        {
            if (_firstLine == null)
            {
                _firstLine = line;
            }
            else if (_firstLine != line)
            {
                var constraint = new ParallelConstraint(_firstLine, line);
                Sketch!.AddConstraint(constraint);
                Editor!.MarkModified();

                _firstLine = null;
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _firstLine = null;
        }
    }

    public override void OnRender(RenderEventArgs e)
    {
        if (_firstLine != null)
        {
            e.Renderer.DrawLine(
                _firstLine.StartPosition,
                _firstLine.EndPosition,
                0x00FFFF,
                3);
        }
    }
}

/// <summary>
/// Tool for adding perpendicular constraints
/// </summary>
public class PerpendicularTool : Tool
{
    private LineEntity? _firstLine;

    public override string Name => "Perpendicular";
    public override string Shortcut => "\\";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var line = GetEntityAtPoint(e.Position) as LineEntity;
        if (line != null)
        {
            if (_firstLine == null)
            {
                _firstLine = line;
            }
            else if (_firstLine != line)
            {
                var constraint = new PerpendicularConstraint(_firstLine, line);
                Sketch!.AddConstraint(constraint);
                Editor!.MarkModified();

                _firstLine = null;
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _firstLine = null;
        }
    }

    public override void OnRender(RenderEventArgs e)
    {
        if (_firstLine != null)
        {
            e.Renderer.DrawLine(
                _firstLine.StartPosition,
                _firstLine.EndPosition,
                0x00FFFF,
                3);
        }
    }
}

/// <summary>
/// Tool for adding tangent constraints
/// </summary>
public class TangentTool : Tool
{
    private Entity? _firstEntity;

    public override string Name => "Tangent";
    public override string Shortcut => "T";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var entity = GetEntityAtPoint(e.Position);
        if (entity != null)
        {
            if (_firstEntity == null)
            {
                _firstEntity = entity;
            }
            else if (_firstEntity != entity)
            {
                Constraint? constraint = null;

                if (_firstEntity is LineEntity line1 && entity is CircleEntity circle1)
                {
                    constraint = new TangentConstraint(line1, circle1);
                }
                else if (_firstEntity is CircleEntity circle2 && entity is LineEntity line2)
                {
                    constraint = new TangentConstraint(line2, circle2);
                }

                if (constraint != null)
                {
                    Sketch!.AddConstraint(constraint);
                    Editor!.MarkModified();
                }

                _firstEntity = null;
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _firstEntity = null;
        }
    }
}

/// <summary>
/// Tool for adding coincident constraints
/// </summary>
public class CoincidentTool : Tool
{
    private PointEntity? _firstPoint;

    public override string Name => "Coincident";
    public override string Shortcut => "O";

    public override void OnMouseMove(MouseEventArgs e)
    {
        // Preview snap
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var entity = GetEntityAtPoint(e.Position);
        PointEntity? point = null;

        if (entity is PointEntity p)
        {
            point = p;
        }
        else if (entity is LineEntity line)
        {
            var closest = line.ClosestPoint(e.Position);
            if (Vec2.Distance(closest, e.Position) < 10)
            {
                // Create point on line
                point = new PointEntity(closest.X, closest.Y);
                Sketch!.AddEntity(point);
            }
        }

        if (point != null)
        {
            if (_firstPoint == null)
            {
                _firstPoint = point;
            }
            else
            {
                var constraint = new CoincidentConstraint(_firstPoint, point);
                Sketch!.AddConstraint(constraint);
                Editor!.MarkModified();

                _firstPoint = null;
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _firstPoint = null;
        }
    }

    public override void OnRender(RenderEventArgs e)
    {
        if (_firstPoint != null)
        {
            e.Renderer.DrawPoint(_firstPoint.Position, 0x00FFFF, 8);
        }
    }
}

/// <summary>
/// Tool for adding distance constraints
/// </summary>
public class DistanceTool : Tool
{
    private Entity? _firstEntity;
    private Entity? _secondEntity;
    private double _lastDistance = 100;

    public override string Name => "Distance";
    public override string Shortcut => "D";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var entity = GetEntityAtPoint(e.Position);
        if (entity != null)
        {
            if (_firstEntity == null)
            {
                _firstEntity = entity;
            }
            else if (_secondEntity == null)
            {
                _secondEntity = entity;
            }
            else
            {
                // Create constraint
                AddDistanceConstraint();
                _firstEntity = null;
                _secondEntity = null;
            }
        }
    }

    private void AddDistanceConstraint()
    {
        if (_firstEntity == null || _secondEntity == null) return;

        Constraint? constraint = null;

        if (_firstEntity is PointEntity p1 && _secondEntity is PointEntity p2)
        {
            constraint = new PointsDistanceConstraint(p1, p2, _lastDistance);
        }

        if (constraint != null)
        {
            Sketch!.AddConstraint(constraint);
            Editor!.MarkModified();
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _firstEntity = null;
            _secondEntity = null;
        }
    }
}

/// <summary>
/// Tool for adding equal constraints
/// </summary>
public class EqualTool : Tool
{
    private Entity? _firstEntity;

    public override string Name => "Equal";
    public override string Shortcut => "Q";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var entity = GetEntityAtPoint(e.Position);
        if (entity != null)
        {
            if (_firstEntity == null)
            {
                _firstEntity = entity;
            }
            else if (_firstEntity != entity)
            {
                Constraint? constraint = null;

                if (_firstEntity is LineEntity l1 && entity is LineEntity l2)
                {
                    constraint = new EqualLengthConstraint(l1, l2);
                }
                else if (_firstEntity is CircleEntity c1 && entity is CircleEntity c2)
                {
                    constraint = new EqualRadiusConstraint(c1, c2);
                }

                if (constraint != null)
                {
                    Sketch!.AddConstraint(constraint);
                    Editor!.MarkModified();
                }

                _firstEntity = null;
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _firstEntity = null;
        }
    }
}

/// <summary>
/// Tool for fixing points
/// </summary>
public class FixTool : Tool
{
    public override string Name => "Fix";
    public override string Shortcut => "F";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        PointEntity? point = null;

        var entity = GetEntityAtPoint(e.Position);
        if (entity is PointEntity p)
        {
            point = p;
        }
        else if (entity is LineEntity line)
        {
            var closest = line.ClosestPoint(e.Position);
            if (Vec2.Distance(closest, e.Position) < 10)
            {
                point = new PointEntity(closest.X, closest.Y);
                Sketch!.AddEntity(point);
            }
        }

        if (point != null)
        {
            var constraint = new FixationConstraint(point, point.Position);
            Sketch!.AddConstraint(constraint);
            Editor!.MarkModified();
        }
    }
}

/// <summary>
/// Tool for adding midpoint constraints
/// </summary>
public class MidpointTool : Tool
{
    private PointEntity? _firstPoint;

    public override string Name => "Midpoint";
    public override string Shortcut => "M";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var entity = GetEntityAtPoint(e.Position);

        if (entity is LineEntity line)
        {
            var midpoint = new PointEntity(
                (line.StartPosition.X + line.EndPosition.X) / 2,
                (line.StartPosition.Y + line.EndPosition.Y) / 2);

            var constraint = new MidpointConstraint(midpoint, line);
            Sketch!.AddEntity(midpoint);
            Sketch!.AddConstraint(constraint);
            Editor!.MarkModified();
        }
        else if (entity is PointEntity point)
        {
            if (_firstPoint == null)
            {
                _firstPoint = point;
            }
            else
            {
                var line = GetEntityAtPoint(e.Position) as LineEntity;
                if (line != null)
                {
                    var constraint = new MidpointConstraint(_firstPoint, line);
                    Sketch!.AddConstraint(constraint);
                    Editor!.MarkModified();
                }
                _firstPoint = null;
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _firstPoint = null;
        }
    }
}

/// <summary>
/// Tool for adding angle constraints
/// </summary>
public class AngleTool : Tool
{
    private LineEntity? _firstLine;
    private LineEntity? _secondLine;
    private double _angle = 90; // Default 90 degrees

    public override string Name => "Angle";
    public override string Shortcut => "G";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var line = GetEntityAtPoint(e.Position) as LineEntity;
        if (line != null)
        {
            if (_firstLine == null)
            {
                _firstLine = line;
            }
            else if (_secondLine == null)
            {
                _secondLine = line;

                // Create angle constraint
                var constraint = new AngleConstraint(_firstLine, _secondLine, _angle * Math.PI / 180);
                Sketch!.AddConstraint(constraint);
                Editor!.MarkModified();

                _firstLine = null;
                _secondLine = null;
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _firstLine = null;
            _secondLine = null;
        }
    }

    public override void OnRender(RenderEventArgs e)
    {
        if (_firstLine != null)
        {
            e.Renderer.DrawLine(
                _firstLine.StartPosition,
                _firstLine.EndPosition,
                0x00FFFF,
                3);
        }
    }
}
