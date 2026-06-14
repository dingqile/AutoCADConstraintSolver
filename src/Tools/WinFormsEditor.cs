using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using AutoCADConstraintSolver.Geometry;

namespace AutoCADConstraintSolver.Tools;

/// <summary>
/// Simple Windows Forms renderer for testing
/// </summary>
public class SimpleRenderer : IRenderer
{
    private readonly Graphics _g;
    private readonly Transform _transform;

    public SimpleRenderer(Graphics g, Transform transform)
    {
        _g = g;
        _transform = transform;
    }

    public void DrawLine(Vec2 start, Vec2 end, uint color, double width = 1)
    {
        using var pen = new Pen(Color.FromArgb((int)color), (float)width);
        var p1 = _transform.ToScreen(start);
        var p2 = _transform.ToScreen(end);
        _g.DrawLine(pen, (float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
    }

    public void DrawCircle(Vec2 center, double radius, uint color, double width = 1)
    {
        using var pen = new Pen(Color.FromArgb((int)color), (float)width);
        var c = _transform.ToScreen(center);
        var r = _transform.ToScreenDistance(radius);
        _g.DrawEllipse(pen, (float)(c.X - r), (float)(c.Y - r), (float)(r * 2), (float)(r * 2));
    }

    public void DrawArc(Vec2 center, double radius, double startAngle, double endAngle, uint color, double width = 1)
    {
        using var pen = new Pen(Color.FromArgb((int)color), (float)width);
        var c = _transform.ToScreen(center);
        var r = _transform.ToScreenDistance(radius);
        var startDeg = (float)(startAngle * 180 / Math.PI);
        var sweepDeg = (float)((endAngle - startAngle) * 180 / Math.PI);
        _g.DrawArc(pen, (float)(c.X - r), (float)(c.Y - r), (float)(r * 2), (float)(r * 2), startDeg, sweepDeg);
    }

    public void DrawPoint(Vec2 position, uint color, double size = 4)
    {
        using var brush = new SolidBrush(Color.FromArgb((int)color));
        var p = _transform.ToScreen(position);
        var s = _transform.ToScreenDistance(size);
        _g.FillEllipse(brush, (float)(p.X - s / 2), (float)(p.Y - s / 2), (float)s, (float)s);
    }

    public void DrawText(string text, Vec2 position, uint color, double size = 12)
    {
        using var font = new Font("Arial", (float)_transform.ToScreenDistance(size));
        using var brush = new SolidBrush(Color.FromArgb((int)color));
        var p = _transform.ToScreen(position);
        _g.DrawString(text, font, brush, (float)p.X, (float)p.Y);
    }

    public void DrawCross(Vec2 position, uint color, double size = 5)
    {
        using var pen = new Pen(Color.FromArgb((int)color));
        var p = _transform.ToScreen(position);
        var s = (float)_transform.ToScreenDistance(size);
        _g.DrawLine(pen, (float)(p.X - s), (float)(p.Y - s), (float)(p.X + s), (float)(p.Y + s));
        _g.DrawLine(pen, (float)(p.X - s), (float)(p.Y + s), (float)(p.X + s), (float)(p.Y - s));
    }

    public void DrawBox(Vec2 min, Vec2 max, uint color)
    {
        using var pen = new Pen(Color.FromArgb((int)color));
        var p1 = _transform.ToScreen(min);
        var p2 = _transform.ToScreen(max);
        _g.DrawRectangle(pen, (float)p1.X, (float)p1.Y, (float)(p2.X - p1.X), (float)(p2.Y - p1.Y));
    }
}

/// <summary>
/// Transform between world and screen coordinates
/// </summary>
public class Transform
{
    public double Scale { get; set; } = 1.0;
    public Vec2 Offset { get; set; } = Vec2.Zero;

    public Vec2 ToScreen(Vec2 world)
    {
        return new Vec2(
            (world.X - Offset.X) * Scale,
            -(world.Y - Offset.Y) * Scale);
    }

    public Vec2 ToWorld(Vec2 screen)
    {
        return new Vec2(
            screen.X / Scale + Offset.X,
            -screen.Y / Scale + Offset.Y);
    }

    public double ToScreenDistance(double worldDistance)
    {
        return worldDistance * Scale;
    }

    public double ToWorldDistance(double screenDistance)
    {
        return screenDistance / Scale;
    }
}

/// <summary>
/// Simple Windows Forms sketch editor form
/// </summary>
public class SketchEditorForm : Form
{
    private readonly SketchEditor _editor;
    private readonly Transform _transform;
    private Tool?[] _tools;
    private Tool? _currentTool;

    public SketchEditorForm()
    {
        Size = new Size(1024, 768);
        Text = "AutoCAD Constraint Solver - Sketch Editor";
        DoubleBuffered = true;

        _editor = new SketchEditor();
        _transform = new Transform { Scale = 0.5, Offset = new Vec2(512, 384) };

        InitializeTools();
        InitializeMenu();

        MouseMove += OnMouseMove;
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
        KeyDown += OnKeyDown;
        Paint += OnPaint;
    }

    private void InitializeTools()
    {
        _tools = new Tool?[]
        {
            new SelectTool(),
            new LineTool(),
            new CircleTool(),
            new ArcTool(),
            new RectTool(),
            new EllipseTool(),
            new PointTool(),
            new MoveTool(),
            new RemoveTool(),
            new HVTool(),
            new ParallelTool(),
            new PerpendicularTool(),
            new TangentTool(),
            new CoincidentTool(),
            new DistanceTool(),
            new EqualTool(),
            new FixTool(),
            new MidpointTool(),
            new AngleTool(),
        };

        _currentTool = _tools[1];
        _editor.SetTool(_currentTool);
    }

    private void InitializeMenu()
    {
        var menu = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add("New", null, (s, e) => NewSketch());
        fileMenu.DropDownItems.Add("Open...", null, (s, e) => OpenSketch());
        fileMenu.DropDownItems.Add("Save", null, (s, e) => SaveSketch());
        fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());

        var toolMenu = new ToolStripMenuItem("Tools");
        var toolNames = new[] { "Select", "Line", "Circle", "Arc", "Rectangle", "Ellipse", "Point",
            "Move", "Remove", "H/V", "Parallel", "Perpendicular", "Tangent",
            "Coincident", "Distance", "Equal", "Fix", "Midpoint", "Angle" };
        
        for (int i = 0; i < _tools.Length && i < toolNames.Length; i++)
        {
            var index = i;
            var item = new ToolStripMenuItem($"{toolNames[i]} ({_tools[i]?.Shortcut})", null, (s, e) => SelectTool(index));
            toolMenu.DropDownItems.Add(item);
        }

        menu.Items.Add(fileMenu);
        menu.Items.Add(toolMenu);
        MainMenuStrip = menu;
        Controls.Add(menu);
    }

    private void SelectTool(int index)
    {
        if (index >= 0 && index < _tools.Length)
        {
            _currentTool = _tools[index];
            _editor.SetTool(_currentTool);
            Text = $"AutoCAD Constraint Solver - {_currentTool?.Name}";
            Invalidate();
        }
    }

    private void NewSketch()
    {
        _editor.Sketch.Clear();
        _editor.SetTool(new SelectTool());
        Invalidate();
    }

    private void OpenSketch()
    {
        using var dialog = new OpenFileDialog { Filter = "DXF Files|*.dxf|All Files|*.*" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var entities = IO.DxfHelper.Import(dialog.FileName);
            foreach (var entity in entities)
            {
                _editor.Sketch.AddEntity(entity);
            }
            Invalidate();
        }
    }

    private void SaveSketch()
    {
        using var dialog = new SaveFileDialog { Filter = "DXF Files|*.dxf|All Files|*.*" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            IO.DxfHelper.Export(dialog.FileName, _editor.Sketch.Entities);
        }
    }

    private Vec2 ScreenToWorld(Point screenPoint)
    {
        return _transform.ToWorld(new Vec2(screenPoint.X, screenPoint.Y));
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        var worldPos = ScreenToWorld(e.Location);
        _editor.OnMouseMove(worldPos);
        Invalidate();
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        var worldPos = ScreenToWorld(e.Location);
        var button = e.Button == MouseButtons.Left ? MouseButton.Left :
                     e.Button == MouseButtons.Right ? MouseButton.Right : MouseButton.Middle;
        _editor.OnMouseDown(button, worldPos);
        Invalidate();
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        var worldPos = ScreenToWorld(e.Location);
        var button = e.Button == MouseButtons.Left ? MouseButton.Left :
                     e.Button == MouseButtons.Right ? MouseButton.Right : MouseButton.Middle;
        _editor.OnMouseUp(button, worldPos);
        Invalidate();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var key = e.KeyCode switch
        {
            Keys.Escape => Key.Escape,
            Keys.Enter => Key.Enter,
            Keys.Delete => Key.Delete,
            Keys.Space => Key.Space,
            _ => Key.None
        };

        var modifiers = Modifiers.None;
        if (e.Shift) modifiers |= Modifiers.Shift;
        if (e.Control) modifiers |= Modifiers.Control;
        if (e.Alt) modifiers |= Modifiers.Alt;

        _editor.OnKeyDown(key, modifiers);
        Invalidate();
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Black);
        var renderer = new SimpleRenderer(e.Graphics, _transform);

        DrawGrid(e.Graphics);

        foreach (var entity in _editor.Sketch.Entities)
        {
            DrawEntity(renderer, entity);
        }

        _editor.CurrentTool?.OnRender(new RenderEventArgs { Renderer = renderer });
    }

    private void DrawGrid(Graphics g)
    {
        using var pen = new Pen(Color.FromArgb(30, 30, 30));

        for (double x = -10000; x < 10000; x += 100)
        {
            var p1 = _transform.ToScreen(new Vec2(x, -10000));
            var p2 = _transform.ToScreen(new Vec2(x, 10000));
            g.DrawLine(pen, (float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
        }

        for (double y = -10000; y < 10000; y += 100)
        {
            var p1 = _transform.ToScreen(new Vec2(-10000, y));
            var p2 = _transform.ToScreen(new Vec2(10000, y));
            g.DrawLine(pen, (float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
        }
    }

    private void DrawEntity(IRenderer renderer, Entities.Entity entity)
    {
        uint color = entity.IsSelected ? 0x00FFFF : 0xFFFFFF;
        double width = entity.IsSelected ? 2 : 1;

        switch (entity)
        {
            case Entities.LineEntity line:
                renderer.DrawLine(line.StartPosition, line.EndPosition, color, width);
                break;

            case Entities.CircleEntity circle:
                renderer.DrawCircle(circle.CenterPosition, circle.RadiusValue, color, width);
                break;

            case Entities.ArcEntity arc:
                renderer.DrawArc(arc.CenterPosition, arc.RadiusValue,
                    arc.StartAngleValue * 180 / Math.PI,
                    arc.EndAngleValue * 180 / Math.PI, color, width);
                break;

            case Entities.PointEntity point:
                renderer.DrawPoint(point.Position, color, 4);
                break;
        }
    }
}

/// <summary>
/// Program entry point
/// </summary>
static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SketchEditorForm());
    }
}
