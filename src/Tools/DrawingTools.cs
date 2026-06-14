using System;
using System.Collections.Generic;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;

namespace AutoCADConstraintSolver.Tools;

/// <summary>
/// Tool for drawing lines
/// </summary>
public class LineTool : Tool
{
    private Vec2? _startPoint;
    private Vec2? _previewEndPoint;
    private LineEntity? _previewLine;

    public override string Name => "Line";
    public override string Shortcut => "L";

    public override void OnActivate()
    {
        _startPoint = null;
        _previewEndPoint = null;
        _previewLine = null;
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        var snapPoint = GetSnapPoint(e.Position);
        _previewEndPoint = snapPoint ?? e.Position;

        // Update preview line
        if (_startPoint.HasValue && _previewEndPoint.HasValue)
        {
            if (_previewLine == null)
            {
                _previewLine = new LineEntity(_startPoint.Value, _previewEndPoint.Value);
            }
            else
            {
                _previewLine.StartPosition = _startPoint.Value;
                _previewLine.EndPosition = _previewEndPoint.Value;
            }
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var snapPoint = GetSnapPoint(e.Position);
        var point = snapPoint ?? e.Position;

        if (!_startPoint.HasValue)
        {
            // First click - set start point
            _startPoint = point;
        }
        else
        {
            // Second click - create line
            var line = new LineEntity(_startPoint.Value, point);
            Sketch!.AddEntity(line);
            Editor!.MarkModified();

            // Continue from end point for chain drawing
            _startPoint = point;
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Cancel line drawing
            _startPoint = null;
            _previewLine = null;
            Editor?.RestorePreviousTool();
        }
        else if (e.Key == Key.Enter || e.Key == Key.Space)
        {
            // Finish line
            _startPoint = null;
            _previewLine = null;
        }
    }

    public override void OnRender(RenderEventArgs e)
    {
        // Draw preview line
        if (_startPoint.HasValue && _previewEndPoint.HasValue && _previewLine != null)
        {
            e.Renderer.DrawLine(
                _previewLine.StartPosition,
                _previewLine.EndPosition,
                0x00FF00, // Green
                2);
        }

        // Draw start point
        if (_startPoint.HasValue)
        {
            e.Renderer.DrawPoint(_startPoint.Value, 0x00FF00, 6);
        }
    }
}

/// <summary>
/// Tool for drawing circles
/// </summary>
public class CircleTool : Tool
{
    private Vec2? _centerPoint;
    private Vec2? _previewRadiusPoint;
    private CircleEntity? _previewCircle;

    public override string Name => "Circle";
    public override string Shortcut => "C";

    public override void OnActivate()
    {
        _centerPoint = null;
        _previewRadiusPoint = null;
        _previewCircle = null;
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        var snapPoint = GetSnapPoint(e.Position);
        _previewRadiusPoint = snapPoint ?? e.Position;

        if (_centerPoint.HasValue && _previewRadiusPoint.HasValue)
        {
            var radius = Vec2.Distance(_centerPoint.Value, _previewRadiusPoint.Value);
            if (_previewCircle == null)
            {
                _previewCircle = new CircleEntity(_centerPoint.Value, radius);
            }
            else
            {
                _previewCircle.CenterPosition = _centerPoint.Value;
                _previewCircle.RadiusValue = radius;
            }
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var snapPoint = GetSnapPoint(e.Position);
        var point = snapPoint ?? e.Position;

        if (!_centerPoint.HasValue)
        {
            _centerPoint = point;
        }
        else
        {
            var radius = Vec2.Distance(_centerPoint.Value, point);
            var circle = new CircleEntity(_centerPoint.Value, radius);
            Sketch!.AddEntity(circle);
            Editor!.MarkModified();

            // Reset for another circle
            _centerPoint = null;
            _previewCircle = null;
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _centerPoint = null;
            _previewCircle = null;
            Editor?.RestorePreviousTool();
        }
    }

    public override void OnRender(RenderEventArgs e)
    {
        if (_previewCircle != null)
        {
            e.Renderer.DrawCircle(
                _previewCircle.CenterPosition,
                _previewCircle.RadiusValue,
                0x00FF00,
                2);
        }

        if (_centerPoint.HasValue)
        {
            e.Renderer.DrawPoint(_centerPoint.Value, 0x00FF00, 6);

            // Draw radius line
            if (_previewRadiusPoint.HasValue)
            {
                e.Renderer.DrawLine(
                    _centerPoint.Value,
                    _previewRadiusPoint.Value,
                    0x0088FF,
                    1);
            }
        }
    }
}

/// <summary>
/// Tool for drawing arcs
/// </summary>
public class ArcTool : Tool
{
    private Vec2? _centerPoint;
    private Vec2? _startPoint;
    private Vec2? _previewEndPoint;
    private double _currentAngle = 0;

    public override string Name => "Arc";
    public override string Shortcut => "A";

    public override void OnActivate()
    {
        _centerPoint = null;
        _startPoint = null;
        _previewEndPoint = null;
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        var snapPoint = GetSnapPoint(e.Position);
        _previewEndPoint = snapPoint ?? e.Position;
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var snapPoint = GetSnapPoint(e.Position);
        var point = snapPoint ?? e.Position;

        if (!_centerPoint.HasValue)
        {
            _centerPoint = point;
        }
        else if (!_startPoint.HasValue)
        {
            _startPoint = point;
        }
        else
        {
            // Create arc
            var radius = Vec2.Distance(_centerPoint.Value, _startPoint.Value);
            var startAngle = Math.Atan2(
                _startPoint.Value.Y - _centerPoint.Value.Y,
                _startPoint.Value.X - _centerPoint.Value.X);
            var endAngle = Math.Atan2(
                point.Y - _centerPoint.Value.Y,
                point.X - _centerPoint.Value.X);

            var arc = new ArcEntity(_centerPoint.Value, radius, startAngle, endAngle);
            Sketch!.AddEntity(arc);
            Editor!.MarkModified();

            // Reset
            _centerPoint = null;
            _startPoint = null;
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _centerPoint = null;
            _startPoint = null;
            Editor?.RestorePreviousTool();
        }
    }

    public override void OnRender(RenderEventArgs e)
    {
        if (_centerPoint.HasValue)
        {
            e.Renderer.DrawPoint(_centerPoint.Value, 0x00FF00, 6);

            if (_startPoint.HasValue && _previewEndPoint.HasValue)
            {
                var radius = Vec2.Distance(_centerPoint.Value, _startPoint.Value);
                var startAngle = Math.Atan2(
                    _startPoint.Value.Y - _centerPoint.Value.Y,
                    _startPoint.Value.X - _centerPoint.Value.X) * 180 / Math.PI;
                var endAngle = Math.Atan2(
                    _previewEndPoint.Value.Y - _centerPoint.Value.Y,
                    _previewEndPoint.Value.X - _centerPoint.Value.X) * 180 / Math.PI;

                e.Renderer.DrawArc(
                    _centerPoint.Value,
                    radius,
                    startAngle,
                    endAngle,
                    0x00FF00,
                    2);

                // Draw radius lines
                e.Renderer.DrawLine(_centerPoint.Value, _startPoint.Value, 0x0088FF, 1);
                e.Renderer.DrawLine(_centerPoint.Value, _previewEndPoint.Value, 0x0088FF, 1);
            }
        }
    }
}

/// <summary>
/// Tool for drawing rectangles
/// </summary>
public class RectTool : Tool
{
    private Vec2? _corner1;
    private Vec2? _corner2;

    public override string Name => "Rectangle";
    public override string Shortcut => "R";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var snapPoint = GetSnapPoint(e.Position);
        var point = snapPoint ?? e.Position;

        if (!_corner1.HasValue)
        {
            _corner1 = point;
        }
        else
        {
            _corner2 = point;

            // Create rectangle (4 lines)
            var p1 = _corner1.Value;
            var p2 = _corner2.Value;

            var p3 = new Vec2(p2.X, p1.Y);
            var p4 = new Vec2(p1.X, p2.Y);

            Sketch!.AddEntity(new LineEntity(p1, p3));
            Sketch!.AddEntity(new LineEntity(p3, p2));
            Sketch!.AddEntity(new LineEntity(p2, p4));
            Sketch!.AddEntity(new LineEntity(p4, p1));

            Editor!.MarkModified();

            _corner1 = null;
            _corner2 = null;
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _corner1 = null;
            _corner2 = null;
            Editor?.RestorePreviousTool();
        }
    }
}

/// <summary>
/// Tool for drawing ellipses
/// </summary>
public class EllipseTool : Tool
{
    private Vec2? _center;
    private Vec2? _majorAxisEnd;

    public override string Name => "Ellipse";
    public override string Shortcut => "E";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var snapPoint = GetSnapPoint(e.Position);
        var point = snapPoint ?? e.Position;

        if (!_center.HasValue)
        {
            _center = point;
        }
        else if (!_majorAxisEnd.HasValue)
        {
            _majorAxisEnd = point;
        }
        else
        {
            // Third click sets minor axis
            var majorRadius = Vec2.Distance(_center.Value, _majorAxisEnd.Value);
            var minorRadius = Vec2.Distance(_center.Value, point);
            var rotation = Math.Atan2(
                _majorAxisEnd.Value.Y - _center.Value.Y,
                _majorAxisEnd.Value.X - _center.Value.X);

            var ellipse = new EllipseEntity(_center.Value, majorRadius, minorRadius, rotation);
            Sketch!.AddEntity(ellipse);
            Editor!.MarkModified();

            _center = null;
            _majorAxisEnd = null;
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _center = null;
            _majorAxisEnd = null;
            Editor?.RestorePreviousTool();
        }
    }
}

/// <summary>
/// Tool for drawing points
/// </summary>
public class PointTool : Tool
{
    public override string Name => "Point";
    public override string Shortcut => "P";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var snapPoint = GetSnapPoint(e.Position);
        var point = snapPoint ?? e.Position;

        var pointEntity = new PointEntity(point.X, point.Y);
        Sketch!.AddEntity(pointEntity);
        Editor!.MarkModified();
    }
}
