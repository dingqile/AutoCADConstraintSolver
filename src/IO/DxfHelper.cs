using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;

namespace AutoCADConstraintSolver.IO;

/// <summary>
/// DXF file reader for importing entities
/// </summary>
public class DxfReader
{
    private readonly List<Entity> _entities = new();
    private string[] _lines = Array.Empty<string>();
    private int _index = 0;

    /// <summary>
    /// Read entities from a DXF file
    /// </summary>
    public List<Entity> Read(string filePath)
    {
        _entities.Clear();
        _lines = File.ReadAllLines(filePath);
        _index = 0;

        while (_index < _lines.Length)
        {
            if (ReadGroupCode() == 0 && ReadStringValue() == "SECTION")
            {
                if (ReadGroupCode() == 2 && ReadStringValue() == "ENTITIES")
                {
                    ReadEntitiesSection();
                }
                else
                {
                    SkipSection();
                }
            }
            else if (ReadGroupCode() == 0 && ReadStringValue() == "EOF")
            {
                break;
            }
        }

        return _entities;
    }

    private void ReadEntitiesSection()
    {
        while (_index < _lines.Length)
        {
            if (ReadGroupCode() == 0)
            {
                var entityType = ReadStringValue();
                
                if (entityType == "ENDSEC")
                    break;
                
                ReadEntity(entityType);
            }
        }
    }

    private void ReadEntity(string entityType)
    {
        switch (entityType)
        {
            case "LINE":
                _entities.Add(ReadLine());
                break;
            case "CIRCLE":
                _entities.Add(ReadCircle());
                break;
            case "ARC":
                _entities.Add(ReadArc());
                break;
            case "ELLIPSE":
                _entities.Add(ReadEllipse());
                break;
            case "LWPOLYLINE":
            case "POLYLINE":
                ReadPolyline();
                break;
        }
    }

    private LineEntity ReadLine()
    {
        double x1 = 0, y1 = 0, x2 = 0, y2 = 0;

        while (_index < _lines.Length)
        {
            var code = ReadGroupCode();
            if (code == 0) break;
            
            if (code == 10) x1 = ReadDoubleValue();
            else if (code == 20) y1 = ReadDoubleValue();
            else if (code == 11) x2 = ReadDoubleValue();
            else if (code == 21) y2 = ReadDoubleValue();
        }

        return new LineEntity(new Vec2(x1, y1), new Vec2(x2, y2));
    }

    private CircleEntity ReadCircle()
    {
        double x = 0, y = 0, radius = 0;

        while (_index < _lines.Length)
        {
            var code = ReadGroupCode();
            if (code == 0) break;
            
            if (code == 10) x = ReadDoubleValue();
            else if (code == 20) y = ReadDoubleValue();
            else if (code == 40) radius = ReadDoubleValue();
        }

        return new CircleEntity(new Vec2(x, y), radius);
    }

    private ArcEntity ReadArc()
    {
        double x = 0, y = 0, radius = 0;
        double startAngle = 0, endAngle = 0;

        while (_index < _lines.Length)
        {
            var code = ReadGroupCode();
            if (code == 0) break;
            
            if (code == 10) x = ReadDoubleValue();
            else if (code == 20) y = ReadDoubleValue();
            else if (code == 40) radius = ReadDoubleValue();
            else if (code == 50) startAngle = ReadDoubleValue() * Math.PI / 180;
            else if (code == 51) endAngle = ReadDoubleValue() * Math.PI / 180;
        }

        return new ArcEntity(new Vec2(x, y), radius, startAngle, endAngle);
    }

    private EllipseEntity ReadEllipse()
    {
        double cx = 0, cy = 0;
        double majorAxisX = 1, majorAxisY = 0;
        double ratio = 1;

        while (_index < _lines.Length)
        {
            var code = ReadGroupCode();
            if (code == 0) break;
            
            if (code == 10) cx = ReadDoubleValue();
            else if (code == 20) cy = ReadDoubleValue();
            else if (code == 11) majorAxisX = ReadDoubleValue();
            else if (code == 21) majorAxisY = ReadDoubleValue();
            else if (code == 40) ratio = ReadDoubleValue();
        }

        var majorAxis = new Vec2(majorAxisX, majorAxisY);
        var rotation = Math.Atan2(majorAxis.Y, majorAxis.X);
        var majorRadius = majorAxis.Magnitude;
        var minorRadius = majorRadius * ratio;

        return new EllipseEntity(new Vec2(cx, cy), majorRadius, minorRadius, rotation);
    }

    private void ReadPolyline()
    {
        var points = new List<Vec2>();
        bool closed = false;

        while (_index < _lines.Length)
        {
            var code = ReadGroupCode();
            if (code == 0) break;
            
            if (code == 70)
            {
                var flags = ReadIntValue();
                closed = (flags & 1) != 0;
            }
            else if (code == 10)
            {
                var x = ReadDoubleValue();
                if (ReadGroupCode() == 20)
                {
                    var y = ReadDoubleValue();
                    points.Add(new Vec2(x, y));
                }
            }
        }

        // Convert polyline to lines
        for (int i = 0; i < points.Count - 1; i++)
        {
            _entities.Add(new LineEntity(points[i], points[i + 1]));
        }
        
        if (closed && points.Count > 2)
        {
            _entities.Add(new LineEntity(points[^1], points[0]));
        }
    }

    private void SkipSection()
    {
        while (_index < _lines.Length)
        {
            if (ReadGroupCode() == 0 && ReadStringValue() == "ENDSEC")
                break;
        }
    }

    private int ReadGroupCode()
    {
        if (_index < _lines.Length && int.TryParse(_lines[_index++].Trim(), out int code))
            return code;
        return 0;
    }

    private string ReadStringValue()
    {
        if (_index < _lines.Length)
            return _lines[_index++].Trim();
        return string.Empty;
    }

    private double ReadDoubleValue()
    {
        if (_index < _lines.Length && double.TryParse(_lines[_index++].Trim(), out double value))
            return value;
        return 0;
    }

    private int ReadIntValue()
    {
        if (_index < _lines.Length && int.TryParse(_lines[_index++].Trim(), out int value))
            return value;
        return 0;
    }
}

/// <summary>
/// DXF file writer for exporting entities
/// </summary>
public class DxfWriter
{
    private readonly StringBuilder _sb = new();
    private int _handle = 100;

    /// <summary>
    /// Write entities to a DXF file
    /// </summary>
    public void Write(string filePath, IEnumerable<Entity> entities)
    {
        _sb.Clear();
        _handle = 100;

        WriteHeader();
        WriteEntitiesSection(entities);
        WriteFooter();

        File.WriteAllText(filePath, _sb.ToString());
    }

    private void WriteHeader()
    {
        Add(0, "SECTION");
        Add(2, "HEADER");
        Add(9, "$ACADVER");
        Add(1, "AC1015");
        Add(9, "$INSUNITS");
        Add(70, 4); // Millimeters
        Add(0, "ENDSEC");
    }

    private void WriteEntitiesSection(IEnumerable<Entity> entities)
    {
        Add(0, "SECTION");
        Add(2, "ENTITIES");

        foreach (var entity in entities)
        {
            switch (entity)
            {
                case LineEntity line:
                    WriteLine(line);
                    break;
                case CircleEntity circle:
                    WriteCircle(circle);
                    break;
                case ArcEntity arc:
                    WriteArc(arc);
                    break;
                case EllipseEntity ellipse:
                    WriteEllipse(ellipse);
                    break;
            }
        }

        Add(0, "ENDSEC");
    }

    private void WriteLine(LineEntity line)
    {
        Add(0, "LINE");
        Add(5, $"{_handle++:X}");
        Add(8, "0");
        Add(10, line.StartPosition.X);
        Add(20, line.StartPosition.Y);
        Add(30, 0);
        Add(11, line.EndPosition.X);
        Add(21, line.EndPosition.Y);
        Add(31, 0);
    }

    private void WriteCircle(CircleEntity circle)
    {
        Add(0, "CIRCLE");
        Add(5, $"{_handle++:X}");
        Add(8, "0");
        Add(10, circle.CenterPosition.X);
        Add(20, circle.CenterPosition.Y);
        Add(30, 0);
        Add(40, circle.RadiusValue);
    }

    private void WriteArc(ArcEntity arc)
    {
        Add(0, "ARC");
        Add(5, $"{_handle++:X}");
        Add(8, "0");
        Add(10, arc.CenterPosition.X);
        Add(20, arc.CenterPosition.Y);
        Add(30, 0);
        Add(40, arc.RadiusValue);
        Add(50, arc.StartAngleValue * 180 / Math.PI);
        Add(51, arc.EndAngleValue * 180 / Math.PI);
    }

    private void WriteEllipse(EllipseEntity ellipse)
    {
        Add(0, "ELLIPSE");
        Add(5, $"{_handle++:X}");
        Add(8, "0");
        Add(10, ellipse.CenterPosition.X);
        Add(20, ellipse.CenterPosition.Y);
        Add(30, 0);
        
        // Major axis endpoint
        var majorAxisLength = ellipse.RadiusXValue;
        var majorAxisX = majorAxisLength * Math.Cos(ellipse.RotationValue);
        var majorAxisY = majorAxisLength * Math.Sin(ellipse.RotationValue);
        Add(11, majorAxisX);
        Add(21, majorAxisY);
        Add(31, 0);
        
        // Ratio of minor to major axis
        Add(40, ellipse.RadiusYValue / ellipse.RadiusXValue);
        Add(41, 0);
        Add(42, 2 * Math.PI);
    }

    private void WriteFooter()
    {
        Add(0, "EOF");
    }

    private void Add(int code, object value)
    {
        _sb.AppendLine(code.ToString().PadLeft(3));
        _sb.AppendLine(value?.ToString() ?? string.Empty);
    }
}

/// <summary>
/// DXF import/export helper
/// </summary>
public static class DxfHelper
{
    /// <summary>
    /// Import entities from a DXF file
    /// </summary>
    public static List<Entity> Import(string filePath)
    {
        var reader = new DxfReader();
        return reader.Read(filePath);
    }

    /// <summary>
    /// Export entities to a DXF file
    /// </summary>
    public static void Export(string filePath, IEnumerable<Entity> entities)
    {
        var writer = new DxfWriter();
        writer.Write(filePath, entities);
    }
}
