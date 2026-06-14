using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Sketch;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace AutoCADConstraintSolver.UI.Controls;

/// <summary>
/// Skia-based canvas for rendering sketch entities
/// </summary>
public class SketchCanvas : SKElement
{
    private readonly Sketch _sketch;
    private SKMatrix _viewMatrix = SKMatrix.CreateIdentity();
    private SKPoint _lastMousePos;
    private bool _isPanning;
    private bool _isDragging;
    private Entity? _hoveredEntity;
    private Vec2 _dragOffset;

    // Colors
    private readonly SKColor _backgroundColor = SKColors.White;
    private readonly SKColor _gridColor = new(230, 230, 230);
    private readonly SKColor _entityColor = SKColors.Black;
    private readonly SKColor _selectedColor = SKColors.DodgerBlue;
    private readonly SKColor _constraintColor = SKColors.Red;
    private readonly SKColor _hoverColor = SKColors.Orange;

    // Settings
    public double GridSpacing { get; set; } = 20;
    public bool ShowGrid { get; set; } = true;
    public double ZoomLevel { get; set; } = 1.0;
    public Vec2 PanOffset { get; set; } = Vec2.Zero;

    public SketchCanvas()
    {
        _sketch = new Sketch();
        _sketch.Modified += () => InvalidateVisual();
        _sketch.SolveCompleted += _ => InvalidateVisual();

        Focusable = true;
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;
        PaintSurface += OnPaintSurface;
    }

    public Sketch Sketch => _sketch;

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;

        canvas.Clear(_backgroundColor);

        // Apply view transform
        canvas.Save();
        canvas.Translate((float)PanOffset.X, (float)PanOffset.Y);
        canvas.Scale((float)ZoomLevel);

        // Draw grid
        if (ShowGrid)
            DrawGrid(canvas, info);

        // Draw entities
        foreach (var entity in _sketch.Entities)
        {
            DrawEntity(canvas, entity);
        }

        // Draw constraints
        foreach (var constraint in _sketch.Constraints)
        {
            DrawConstraint(canvas, constraint);
        }

        canvas.Restore();
    }

    private void DrawGrid(SKCanvas canvas, SKImageInfo info)
    {
        using var paint = new SKPaint
        {
            Color = _gridColor,
            StrokeWidth = 1,
            IsAntialias = true
        };

        var width = info.Width / (float)ZoomLevel;
        var height = info.Height / (float)ZoomLevel;
        var startX = -(float)PanOffset.X / (float)ZoomLevel;
        var startY = -(float)PanOffset.Y / (float)ZoomLevel;

        // Vertical lines
        var firstX = (float)Math.Floor(startX / GridSpacing) * GridSpacing;
        for (var x = firstX; x < startX + width; x += GridSpacing)
        {
            canvas.DrawLine((float)x, 0, (float)x, (float)height, paint);
        }

        // Horizontal lines
        var firstY = (float)Math.Floor(startY / GridSpacing) * GridSpacing;
        for (var y = firstY; y < startY + height; y += GridSpacing)
        {
            canvas.DrawLine(0, (float)y, (float)width, (float)y, paint);
        }
    }

    private void DrawEntity(SKCanvas canvas, Entity entity)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            StrokeWidth = 2
        };

        if (entity.IsSelected)
        {
            paint.Color = _selectedColor;
            paint.Style = SKPaintStyle.StrokeAndFill;
            paint.Alpha = 50;
        }
        else if (entity == _hoveredEntity)
        {
            paint.Color = _hoverColor;
        }
        else
        {
            paint.Color = _entityColor;
        }

        switch (entity)
        {
            case LineEntity line:
                DrawLine(canvas, line, paint);
                break;
            case CircleEntity circle:
                DrawCircle(canvas, circle, paint);
                break;
            case ArcEntity arc:
                DrawArc(canvas, arc, paint);
                break;
            case PointEntity point:
                DrawPoint(canvas, point, paint);
                break;
        }
    }

    private void DrawLine(SKCanvas canvas, LineEntity line, SKPaint paint)
    {
        canvas.DrawLine(
            (float)line.StartPosition.X,
            (float)line.StartPosition.Y,
            (float)line.EndPosition.X,
            (float)line.EndPosition.Y,
            paint);
    }

    private void DrawCircle(SKCanvas canvas, CircleEntity circle, SKPaint paint)
    {
        canvas.DrawCircle(
            (float)circle.CenterPosition.X,
            (float)circle.CenterPosition.Y,
            (float)circle.RadiusValue,
            paint);
    }

    private void DrawArc(SKCanvas canvas, ArcEntity arc, SKPaint paint)
    {
        var rect = new SKRect(
            (float)(arc.CenterPosition.X - arc.RadiusValue),
            (float)(arc.CenterPosition.Y - arc.RadiusValue),
            (float)(arc.CenterPosition.X + arc.RadiusValue),
            (float)(arc.CenterPosition.Y + arc.RadiusValue));

        var startAngle = (float)(arc.StartAngleValue * 180 / Math.PI);
        var sweepAngle = (float)(arc.SweepAngle * 180 / Math.PI);

        using var path = new SKPath();
        path.MoveTo((float)arc.StartPoint.X, (float)arc.StartPoint.Y);
        path.ArcTo(rect, startAngle, sweepAngle, false);
        
        canvas.DrawPath(path, paint);
    }

    private void DrawPoint(SKCanvas canvas, PointEntity point, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Fill;
        canvas.DrawCircle(
            (float)point.Position.X,
            (float)point.Position.Y,
            5,
            paint);
    }

    private void DrawConstraint(SKCanvas canvas, Constraint constraint)
    {
        using var paint = new SKPaint
        {
            Color = _constraintColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };

        var entities = constraint.GetEntities().ToList();
        if (entities.Count < 2) return;

        switch (constraint)
        {
            case Constraints.CoincidentConstraint coincident:
                DrawCoincident(canvas, coincident, paint);
                break;
            case Constraints.HorizontalConstraint:
            case Constraints.VerticalConstraint:
            case Constraints.ParallelConstraint:
            case Constraints.PerpendicularConstraint:
                DrawLineConstraint(canvas, constraint, paint);
                break;
            case Constraints.PointsDistanceConstraint dist:
                DrawDistance(canvas, dist, paint);
                break;
        }
    }

    private void DrawCoincident(SKCanvas canvas, Constraints.CoincidentConstraint c, SKPaint paint)
    {
        var entities = c.GetEntities().ToList();
        if (entities.Count == 2)
        {
            var pos = entities[1] switch
            {
                PointEntity p => p.Position,
                _ => Vec2.Zero
            };
            canvas.DrawCircle((float)pos.X, (float)pos.Y, 4, paint);
        }
    }

    private void DrawLineConstraint(SKCanvas canvas, Constraint constraint, SKPaint paint)
    {
        var entities = constraint.GetEntities().ToList();
        if (entities.Count >= 1 && entities[0] is LineEntity line)
        {
            var mid = line.GetPointAt(0.5);
            canvas.DrawCircle((float)mid.X, (float)mid.Y, 3, paint);
        }
    }

    private void DrawDistance(SKCanvas canvas, Constraints.PointsDistanceConstraint dist, SKPaint paint)
    {
        var entities = dist.GetEntities().ToList();
        if (entities.Count == 2 && entities[0] is PointEntity p1 && entities[1] is PointEntity p2)
        {
            var mid = Vec2.Lerp(p1.Position, p2.Position, 0.5);
            DrawDimension(canvas, (float)mid.X, (float)mid.Y, dist.Distance, paint);
        }
    }

    private void DrawDimension(SKCanvas canvas, float x, float y, double value, SKPaint paint)
    {
        var text = $"{value:F1}";
        using var tpaint = new SKPaint
        {
            Color = _constraintColor,
            IsAntialias = true,
            TextSize = 12
        };
        canvas.DrawText(text, x + 5, y - 5, tpaint);
    }

    private Entity? HitTest(Vec2 worldPos)
    {
        foreach (var entity in _sketch.Entities)
        {
            var dist = entity.DistanceTo(worldPos);
            if (dist < 10 / ZoomLevel)
                return entity;
        }
        return null;
    }

    private Vec2 ScreenToWorld(Point screenPos)
    {
        var x = (screenPos.X - PanOffset.X) / ZoomLevel;
        var y = (screenPos.Y - PanOffset.Y) / ZoomLevel;
        return new Vec2(x, y);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        _lastMousePos = new SKPoint((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var worldPos = ScreenToWorld(e.GetPosition(this));
            var hit = HitTest(worldPos);

            if (hit != null)
            {
                _sketch.DeselectAll();
                hit.IsSelected = true;
                _isDragging = true;
                _dragOffset = worldPos - hit.GetPosition();
            }
            else
            {
                _sketch.DeselectAll();
                _isPanning = true;
            }
        }

        InvalidateVisual();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        _isDragging = false;
        InvalidateVisual();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var pos = new SKPoint((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);
        var delta = pos - _lastMousePos;

        if (_isPanning)
        {
            PanOffset = new Vec2(PanOffset.X + delta.X, PanOffset.Y + delta.Y);
            InvalidateVisual();
        }
        else if (_isDragging)
        {
            var worldDelta = new Vec2(delta.X / ZoomLevel, delta.Y / ZoomLevel);
            _sketch.MoveSelected(worldDelta);
            _sketch.Solve();
            InvalidateVisual();
        }
        else
        {
            var worldPos = ScreenToWorld(e.GetPosition(this));
            var hit = HitTest(worldPos);
            if (hit != _hoveredEntity)
            {
                _hoveredEntity = hit;
                InvalidateVisual();
            }
        }

        _lastMousePos = pos;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var factor = e.Delta > 0 ? 1.1 : 0.9;
        ZoomLevel *= factor;
        ZoomLevel = Math.Clamp(ZoomLevel, 0.1, 10);
        InvalidateVisual();
    }

    public void AddLine(Vec2 start, Vec2 end)
    {
        _sketch.AddEntity(new LineEntity(start, end));
        InvalidateVisual();
    }

    public void AddCircle(Vec2 center, double radius)
    {
        _sketch.AddEntity(new CircleEntity(center, radius));
        InvalidateVisual();
    }

    public void Solve()
    {
        _sketch.Solve();
        InvalidateVisual();
    }
}
