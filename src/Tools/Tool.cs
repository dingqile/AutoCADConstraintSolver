using System;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Sketch;

namespace AutoCADConstraintSolver.Tools;

/// <summary>
/// Base class for all tools
/// </summary>
public abstract class Tool
{
    /// <summary>
    /// Tool name
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Tool description
    /// </summary>
    public virtual string Description => Name;

    /// <summary>
    /// Icon name for the toolbar
    /// </summary>
    public virtual string Icon => Name;

    /// <summary>
    /// Keyboard shortcut
    /// </summary>
    public virtual string Shortcut => "";

    /// <summary>
    /// Reference to the sketch editor
    /// </summary>
    protected SketchEditor? Editor { get; private set; }

    /// <summary>
    /// Current sketch
    /// </summary>
    protected Sketch? Sketch => Editor?.Sketch;

    /// <summary>
    /// Initialize the tool with the editor
    /// </summary>
    public virtual void Initialize(SketchEditor editor)
    {
        Editor = editor;
    }

    /// <summary>
    /// Called when the tool is activated
    /// </summary>
    public virtual void OnActivate() { }

    /// <summary>
    /// Called when the tool is deactivated
    /// </summary>
    public virtual void OnDeactivate() { }

    /// <summary>
    /// Called when the mouse moves
    /// </summary>
    public virtual void OnMouseMove(MouseEventArgs e) { }

    /// <summary>
    /// Called when the left mouse button is pressed
    /// </summary>
    public virtual void OnMouseDown(MouseEventArgs e) { }

    /// <summary>
    /// Called when the left mouse button is released
    /// </summary>
    public virtual void OnMouseUp(MouseEventArgs e) { }

    /// <summary>
    /// Called when the right mouse button is pressed
    /// </summary>
    public virtual void OnContextMenu(ContextMenuEventArgs e) { }

    /// <summary>
    /// Called when a key is pressed
    /// </summary>
    public virtual void OnKeyDown(KeyEventArgs e) { }

    /// <summary>
    /// Called when a key is released
    /// </summary>
    public virtual void OnKeyUp(KeyEventArgs e) { }

    /// <summary>
    /// Called when the tool needs to render
    /// </summary>
    public virtual void OnRender(RenderEventArgs e) { }

    /// <summary>
    /// Check if an entity is selected under the cursor
    /// </summary>
    protected Entity? GetEntityAtPoint(Vec2 point)
    {
        if (Sketch == null) return null;

        double minDist = double.MaxValue;
        Entity? closest = null;

        foreach (var entity in Sketch.Entities)
        {
            var dist = entity.DistanceTo(point);
            if (dist < minDist && dist < 10) // 10 pixel threshold
            {
                minDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    /// <summary>
    /// Get snap point at cursor position
    /// </summary>
    protected Vec2? GetSnapPoint(Vec2 cursorPos)
    {
        if (Sketch == null) return null;

        // Snap to existing points
        foreach (var entity in Sketch.Entities)
        {
            if (entity is PointEntity point)
            {
                if (Vec2.Distance(cursorPos, point.Position) < 5)
                    return point.Position;
            }
            else if (entity is LineEntity line)
            {
                var closest = line.ClosestPoint(cursorPos);
                if (Vec2.Distance(cursorPos, closest) < 5)
                    return closest;
            }
            else if (entity is CircleEntity circle)
            {
                var closest = circle.ClosestPointTo(cursorPos);
                if (Vec2.Distance(cursorPos, closest) < 5)
                    return closest;
            }
        }

        return null;
    }
}

/// <summary>
/// Mouse event arguments
/// </summary>
public class MouseEventArgs
{
    public Vec2 Position { get; set; }
    public int Clicks { get; set; }
    public MouseButton Button { get; set; }
    public bool Handled { get; set; }
}

/// <summary>
/// Mouse button
/// </summary>
public enum MouseButton
{
    Left,
    Middle,
    Right
}

/// <summary>
/// Key event arguments
/// </summary>
public class KeyEventArgs
{
    public Key Key { get; set; }
    public Modifiers Modifiers { get; set; }
    public bool Handled { get; set; }
}

/// <summary>
/// Keyboard key
/// </summary>
public enum Key
{
    None,
    Enter,
    Escape,
    Delete,
    Back,
    Tab,
    Space,
    Left,
    Right,
    Up,
    Down,
    Shift,
    Control,
    Alt
}

/// <summary>
/// Keyboard modifiers
/// </summary>
[Flags]
public enum Modifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4
}

/// <summary>
/// Context menu event arguments
/// </summary>
public class ContextMenuEventArgs
{
    public Vec2 Position { get; set; }
    public List<MenuItem> Items { get; } = new();
}

/// <summary>
/// Menu item
/// </summary>
public class MenuItem
{
    public string Text { get; set; } = "";
    public Action? Action { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Shortcut { get; set; }
}

/// <summary>
/// Render event arguments
/// </summary>
public class RenderEventArgs
{
    public IRenderer Renderer { get; set; } = null!;
}

/// <summary>
/// Interface for rendering
/// </summary>
public interface IRenderer
{
    void DrawLine(Vec2 start, Vec2 end, uint color, double width = 1);
    void DrawCircle(Vec2 center, double radius, uint color, double width = 1);
    void DrawArc(Vec2 center, double radius, double startAngle, double endAngle, uint color, double width = 1);
    void DrawPoint(Vec2 position, uint color, double size = 4);
    void DrawText(string text, Vec2 position, uint color, double size = 12);
    void DrawCross(Vec2 position, uint color, double size = 5);
    void DrawBox(Vec2 min, Vec2 max, uint color);
}

/// <summary>
/// Sketch editor - main UI controller
/// </summary>
public class SketchEditor
{
    private Tool? _currentTool;
    private Tool? _previousTool;

    public Sketch Sketch { get; } = new();
    public Tool? CurrentTool => _currentTool;
    public Vec2 CursorPosition { get; private set; }
    public bool IsBusy { get; set; }

    public event Action<Tool>? ToolChanged;
    public event Action? Modified;

    public void SetTool(Tool? tool)
    {
        _previousTool = _currentTool;
        _currentTool?.OnDeactivate();
        _currentTool = tool;
        _currentTool?.Initialize(this);
        _currentTool?.OnActivate();
        ToolChanged?.Invoke(_currentTool!);
    }

    public void RestorePreviousTool()
    {
        SetTool(_previousTool);
    }

    public void OnMouseMove(Vec2 position)
    {
        CursorPosition = position;
        _currentTool?.OnMouseMove(new MouseEventArgs { Position = position });
    }

    public void OnMouseDown(MouseButton button, Vec2 position)
    {
        _currentTool?.OnMouseDown(new MouseEventArgs 
        { 
            Position = position, 
            Button = button,
            Clicks = 1
        });
    }

    public void OnMouseUp(MouseButton button, Vec2 position)
    {
        _currentTool?.OnMouseUp(new MouseEventArgs 
        { 
            Position = position, 
            Button = button 
        });
    }

    public void OnKeyDown(Key key, Modifiers modifiers)
    {
        _currentTool?.OnKeyDown(new KeyEventArgs { Key = key, Modifiers = modifiers });
    }

    public void OnKeyUp(Key key, Modifiers modifiers)
    {
        _currentTool?.OnKeyUp(new KeyEventArgs { Key = key, Modifiers = modifiers });
    }

    public void MarkModified()
    {
        Modified?.Invoke();
    }
}
