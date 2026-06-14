using System;
using System.Collections.Generic;
using System.Linq;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Constraints;

namespace AutoCADConstraintSolver.Tools;

/// <summary>
/// Tool for selecting entities
/// </summary>
public class SelectTool : Tool
{
    private Vec2? _dragStart;
    private Vec2? _dragEnd;
    private bool _isDragging;
    private Entity? _hoveredEntity;
    private Vec2? _hoverPoint;

    public override string Name => "Select";
    public override string Shortcut => "S";

    public override void OnMouseMove(MouseEventArgs e)
    {
        // Find hovered entity
        _hoveredEntity = GetEntityAtPoint(e.Position);
        _hoverPoint = GetSnapPoint(e.Position);

        // Handle dragging for box selection
        if (_isDragging && _dragStart.HasValue)
        {
            _dragEnd = e.Position;
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButton.Left)
        {
            var entity = GetEntityAtPoint(e.Position);
            var snapPoint = GetSnapPoint(e.Position);

            if (entity != null)
            {
                // Toggle selection
                if (e.Modifiers.HasFlag(Modifiers.Control))
                {
                    entity.IsSelected = !entity.IsSelected;
                }
                else
                {
                    // Clear other selections
                    foreach (var ent in Sketch!.Entities)
                    {
                        ent.IsSelected = false;
                    }
                    entity.IsSelected = true;
                }
            }
            else if (snapPoint.HasValue)
            {
                // Clicked on a snap point
                if (e.Modifiers.HasFlag(Modifiers.Control))
                {
                    // Find entity at snap point and toggle
                    foreach (var ent in Sketch!.Entities)
                    {
                        if (ent.DistanceTo(snapPoint.Value) < 10)
                        {
                            ent.IsSelected = !ent.IsSelected;
                        }
                    }
                }
            }
            else
            {
                // Start box selection
                if (!e.Modifiers.HasFlag(Modifiers.Control))
                {
                    // Clear selection
                    foreach (var ent in Sketch!.Entities)
                    {
                        ent.IsSelected = false;
                    }
                }
                _isDragging = true;
                _dragStart = e.Position;
                _dragEnd = e.Position;
            }

            Editor!.MarkModified();
        }
        else if (e.Button == MouseButton.Right)
        {
            // Show context menu
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        if (_isDragging && _dragStart.HasValue && _dragEnd.HasValue)
        {
            // Select entities in box
            var minX = Math.Min(_dragStart.Value.X, _dragEnd.Value.X);
            var maxX = Math.Max(_dragStart.Value.X, _dragEnd.Value.X);
            var minY = Math.Min(_dragStart.Value.Y, _dragEnd.Value.Y);
            var maxY = Math.Max(_dragStart.Value.Y, _dragEnd.Value.Y);

            foreach (var entity in Sketch!.Entities)
            {
                var bbox = entity.GetBoundingBox();
                if (bbox.Min.X >= minX && bbox.Max.X <= maxX &&
                    bbox.Min.Y >= minY && bbox.Max.Y <= maxY)
                {
                    entity.IsSelected = true;
                }
            }

            _isDragging = false;
            _dragStart = null;
            _dragEnd = null;
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            // Delete selected entities
            var toDelete = Sketch!.Entities.Where(x => x.IsSelected).ToList();
            foreach (var entity in toDelete)
            {
                Sketch.RemoveEntity(entity);
            }
            Editor!.MarkModified();
        }
        else if (e.Key == Key.Escape)
        {
            // Deselect all
            foreach (var entity in Sketch!.Entities)
            {
                entity.IsSelected = false;
            }
            Editor!.MarkModified();
        }
    }

    public override void OnRender(RenderEventArgs e)
    {
        // Highlight hovered entity
        if (_hoveredEntity != null)
        {
            HighlightEntity(e.Renderer, _hoveredEntity, 0xFFFF00); // Yellow
        }

        // Highlight hovered snap point
        if (_hoverPoint.HasValue)
        {
            e.Renderer.DrawCross(_hoverPoint.Value, 0xFFFF00, 8);
        }

        // Draw selection box
        if (_isDragging && _dragStart.HasValue && _dragEnd.HasValue)
        {
            var min = new Vec2(
                Math.Min(_dragStart.Value.X, _dragEnd.Value.X),
                Math.Min(_dragStart.Value.Y, _dragEnd.Value.Y));
            var max = new Vec2(
                Math.Max(_dragStart.Value.X, _dragEnd.Value.X),
                Math.Max(_dragStart.Value.Y, _dragEnd.Value.Y));

            e.Renderer.DrawBox(min, max, 0x00FFFF); // Cyan
        }

        // Draw selection handles for selected entities
        foreach (var entity in Sketch!.Entities.Where(x => x.IsSelected))
        {
            DrawSelectionHandles(e.Renderer, entity);
        }
    }

    private void HighlightEntity(IRenderer renderer, Entity entity, uint color)
    {
        switch (entity)
        {
            case LineEntity line:
                renderer.DrawLine(line.StartPosition, line.EndPosition, color, 3);
                break;
            case CircleEntity circle:
                renderer.DrawCircle(circle.CenterPosition, circle.RadiusValue, color, 3);
                break;
            case ArcEntity arc:
                renderer.DrawArc(arc.CenterPosition, arc.RadiusValue,
                    arc.StartAngleValue * 180 / Math.PI,
                    arc.EndAngleValue * 180 / Math.PI, color, 3);
                break;
        }
    }

    private void DrawSelectionHandles(IRenderer renderer, Entity entity)
    {
        uint handleColor = 0x0000FF; // Blue
        double handleSize = 6;

        switch (entity)
        {
            case LineEntity line:
                renderer.DrawPoint(line.StartPosition, handleColor, handleSize);
                renderer.DrawPoint(line.EndPosition, handleColor, handleSize);
                break;

            case CircleEntity circle:
                renderer.DrawPoint(circle.CenterPosition, handleColor, handleSize);
                // Draw radius handle
                var radiusPoint = circle.CenterPosition + new Vec2(circle.RadiusValue, 0);
                renderer.DrawPoint(radiusPoint, handleColor, handleSize);
                break;

            case PointEntity point:
                renderer.DrawPoint(point.Position, handleColor, handleSize * 1.5);
                break;
        }
    }
}

/// <summary>
/// Tool for moving selected entities
/// </summary>
public class MoveTool : Tool
{
    private Vec2? _startPos;
    private Vec2? _currentPos;
    private Vec2? _originalPosition;

    public override string Name => "Move";
    public override string Shortcut => "M";

    public override void OnActivate()
    {
        _startPos = null;
        _currentPos = null;
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var entity = GetEntityAtPoint(e.Position);
        if (entity != null && entity.IsSelected)
        {
            _startPos = e.Position;
            _originalPosition = entity.GetPosition();
        }
        else if (entity != null)
        {
            // Select and start move
            foreach (var ent in Sketch!.Entities) ent.IsSelected = false;
            entity.IsSelected = true;
            _startPos = e.Position;
            _originalPosition = entity.GetPosition();
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        if (_startPos.HasValue)
        {
            _currentPos = GetSnapPoint(e.Position) ?? e.Position;

            var delta = _currentPos.Value - _startPos.Value;

            // Move all selected entities
            foreach (var entity in Sketch!.Entities.Where(x => x.IsSelected))
            {
                entity.Move(delta);
            }
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        if (_startPos.HasValue)
        {
            Editor!.MarkModified();
        }
        _startPos = null;
        _currentPos = null;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Revert move
            if (_originalPosition.HasValue)
            {
                foreach (var entity in Sketch!.Entities.Where(x => x.IsSelected))
                {
                    entity.Move(_originalPosition.Value - entity.GetPosition());
                }
            }
            Editor?.RestorePreviousTool();
        }
    }
}

/// <summary>
/// Tool for copying entities
/// </summary>
public class CopyTool : Tool
{
    private Vec2? _startPos;
    private Vec2? _currentPos;

    public override string Name => "Copy";
    public override string Shortcut => "Ctrl+C";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var entity = GetEntityAtPoint(e.Position);
        if (entity != null)
        {
            _startPos = GetSnapPoint(e.Position) ?? e.Position;

            // Clone entity
            var clone = entity.Clone();
            clone.IsSelected = true;
            Sketch!.AddEntity(clone);
            entity.IsSelected = false;
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        if (_startPos.HasValue)
        {
            _currentPos = GetSnapPoint(e.Position) ?? e.Position;
            var delta = _currentPos.Value - _startPos.Value;

            foreach (var entity in Sketch!.Entities.Where(x => x.IsSelected))
            {
                entity.Move(delta);
            }
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        _startPos = null;
        _currentPos = null;
        Editor!.MarkModified();
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Delete copied entities
            var toDelete = Sketch!.Entities.Where(x => x.IsSelected).ToList();
            foreach (var entity in toDelete)
            {
                Sketch!.RemoveEntity(entity);
            }
            Editor?.RestorePreviousTool();
        }
    }
}

/// <summary>
/// Tool for removing entities
/// </summary>
public class RemoveTool : Tool
{
    public override string Name => "Remove";
    public override string Shortcut => "Del";

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButton.Left) return;

        var entity = GetEntityAtPoint(e.Position);
        if (entity != null)
        {
            Sketch!.RemoveEntity(entity);
            Editor!.MarkModified();
        }
    }
}
