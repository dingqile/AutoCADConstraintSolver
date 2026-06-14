using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;

namespace AutoCADConstraintSolver.Converter;

/// <summary>
/// Sketch document that syncs with AutoCAD
/// </summary>
public class SketchDocument
{
    private readonly Sketch.Sketch _sketch;
    private readonly Dictionary<ObjectId, Entity> _idMap = new();
    private readonly Dictionary<Entity, ObjectId> _reverseMap = new();

    public Sketch.Sketch Sketch => _sketch;

    public SketchDocument()
    {
        _sketch = new Sketch.Sketch();
        _sketch.Modified += OnSketchModified;
        _sketch.SolveCompleted += OnSolveCompleted;
    }

    private void OnSketchModified()
    {
        // Could trigger AutoCAD redraw here
    }

    private void OnSolveCompleted(Solver.SolveResult result)
    {
        // Could update AutoCAD entities here
    }

    /// <summary>
    /// Import entities from AutoCAD model space
    /// </summary>
    public void ImportFromAutoCAD()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null) return;

        var db = doc.Database;

        using var tr = db.TransactionManager.StartTransaction();
        var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

        if (bt == null) return;

        var msId = bt[BlockTableRecord.ModelSpace];
        var ms = tr.GetObject(msId, OpenMode.ForRead) as BlockTableRecord;

        if (ms == null) return;

        foreach (ObjectId objId in ms)
        {
            var acadEntity = tr.GetObject(objId, OpenMode.ForRead);
            var internalEntity = ConvertToInternal(acadEntity);

            if (internalEntity != null)
            {
                _sketch.AddEntity(internalEntity);
                _idMap[objId] = internalEntity;
                _reverseMap[internalEntity] = objId;
            }
        }

        tr.Commit();
    }

    /// <summary>
    /// Sync all entities back to AutoCAD
    /// </summary>
    public void SyncToAutoCAD()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null) return;

        var db = doc.Database;

        using var tr = db.TransactionManager.StartTransaction();

        foreach (var (entity, objId) in _reverseMap)
        {
            var acadEntity = tr.GetObject(objId, OpenMode.ForWrite);
            UpdateAutoCADEntity(entity, acadEntity);
        }

        tr.Commit();
    }

    /// <summary>
    /// Convert AutoCAD entity to internal entity
    /// </summary>
    public Entity? ConvertToInternal(object acadEntity)
    {
        switch (acadEntity)
        {
            case Line line:
                return ConvertLine(line);
            case Circle circle:
                return ConvertCircle(circle);
            case Arc arc:
                return ConvertArc(arc);
            default:
                return null;
        }
    }

    private LineEntity ConvertLine(Line line)
    {
        var start = new Vec2(line.StartPoint.X, line.StartPoint.Y);
        var end = new Vec2(line.EndPoint.X, line.EndPoint.Y);
        return new LineEntity(start, end);
    }

    private CircleEntity ConvertCircle(Circle circle)
    {
        var center = new Vec2(circle.Center.X, circle.Center.Y);
        return new CircleEntity(center, circle.Radius);
    }

    private ArcEntity ConvertArc(Arc arc)
    {
        var center = new Vec2(arc.Center.X, arc.Center.Y);
        var startAngle = arc.StartAngle * Math.PI / 180.0;
        var endAngle = arc.EndAngle * Math.PI / 180.0;
        return new ArcEntity(center, arc.Radius, startAngle, endAngle);
    }

    /// <summary>
    /// Update AutoCAD entity from internal entity
    /// </summary>
    public void UpdateAutoCADEntity(Entity entity, object acadEntity)
    {
        switch (entity)
        {
            case LineEntity line:
                if (acadEntity is Line lineObj)
                {
                    lineObj.StartPoint = new Point3d(line.StartPosition.X, line.StartPosition.Y, 0);
                    lineObj.EndPoint = new Point3d(line.EndPosition.X, line.EndPosition.Y, 0);
                }
                break;

            case CircleEntity circle:
                if (acadEntity is Circle circleObj)
                {
                    circleObj.Center = new Point3d(circle.CenterPosition.X, circle.CenterPosition.Y, 0);
                    circleObj.Radius = circle.RadiusValue;
                }
                break;

            case ArcEntity arc:
                if (acadEntity is Arc arcObj)
                {
                    arcObj.Center = new Point3d(arc.CenterPosition.X, arc.CenterPosition.Y, 0);
                    arcObj.Radius = arc.RadiusValue;
                    arcObj.StartAngle = arc.StartAngleValue * 180 / Math.PI;
                    arcObj.EndAngle = arc.EndAngleValue * 180 / Math.PI;
                }
                break;
        }
    }
}

/// <summary>
/// Interface for AutoCAD entity conversion
/// </summary>
public interface IAutoCADConverter
{
    /// <summary>
    /// Convert an AutoCAD entity to internal entity
    /// </summary>
    Entity? ConvertToInternal(object acadEntity);

    /// <summary>
    /// Convert internal entity to AutoCAD entity
    /// </summary>
    object? ConvertToAutoCAD(Entity entity);

    /// <summary>
    /// Update AutoCAD entity from internal entity
    /// </summary>
    void UpdateAutoCAD(Entity entity, object acadEntity);

    /// <summary>
    /// Get the ObjectId for an entity
    /// </summary>
    ObjectId? GetObjectId(Entity entity);

    /// <summary>
    /// Find internal entity by ObjectId
    /// </summary>
    Entity? FindByObjectId(ObjectId objectId);
}

/// <summary>
/// Factory for creating converters based on AutoCAD version
/// </summary>
public static class ConverterFactory
{
    public static IAutoCADConverter CreateConverter(string acadVersion)
    {
        return acadVersion switch
        {
            "2021" => new AutoCAD2021Converter(),
            "2022" => new AutoCAD2022Converter(),
            "2023" => new AutoCAD2023Converter(),
            "2024" => new AutoCAD2024Converter(),
            "2025" => new AutoCAD2025Converter(),
            "2026" => new AutoCAD2026Converter(),
            _ => new AutoCAD2026Converter()
        };
    }
}

/// <summary>
/// Base converter class for AutoCAD 2021-2026
/// </summary>
public abstract class AutoCADConverterBase : IAutoCADConverter
{
    protected readonly Dictionary<ObjectId, Entity> _objectIdToEntity = new();
    protected readonly Dictionary<Entity, ObjectId> _entityToObjectId = new();

    public virtual Entity? ConvertToInternal(object acadEntity)
    {
        var typeName = acadEntity.GetType().Name;

        return typeName switch
        {
            "Line" => ConvertLineToInternal(acadEntity),
            "Circle" => ConvertCircleToInternal(acadEntity),
            "Arc" => ConvertArcToInternal(acadEntity),
            "Ellipse" => ConvertEllipseToInternal(acadEntity),
            _ => null
        };
    }

    protected abstract LineEntity ConvertLineToInternal(object line);
    protected abstract CircleEntity ConvertCircleToInternal(object circle);
    protected abstract ArcEntity ConvertArcToInternal(object arc);
    protected abstract Entity? ConvertEllipseToInternal(object ellipse);

    public virtual object? ConvertToAutoCAD(Entity entity)
    {
        return entity switch
        {
            LineEntity line => ConvertLineToAutoCAD(line),
            CircleEntity circle => ConvertCircleToAutoCAD(circle),
            ArcEntity arc => ConvertArcToAutoCAD(arc),
            _ => null
        };
    }

    protected abstract object ConvertLineToAutoCAD(LineEntity line);
    protected abstract object ConvertCircleToAutoCAD(CircleEntity circle);
    protected abstract object ConvertArcToAutoCAD(ArcEntity arc);

    public virtual void UpdateAutoCAD(Entity entity, object acadEntity)
    {
        switch (entity)
        {
            case LineEntity line when acadEntity is Line lineObj:
                lineObj.StartPoint = new Point3d(line.StartPosition.X, line.StartPosition.Y, 0);
                lineObj.EndPoint = new Point3d(line.EndPosition.X, line.EndPosition.Y, 0);
                break;

            case CircleEntity circle when acadEntity is Circle circleObj:
                circleObj.Center = new Point3d(circle.CenterPosition.X, circle.CenterPosition.Y, 0);
                circleObj.Radius = circle.RadiusValue;
                break;

            case ArcEntity arc when acadEntity is Arc arcObj:
                arcObj.Center = new Point3d(arc.CenterPosition.X, arc.CenterPosition.Y, 0);
                arcObj.Radius = arc.RadiusValue;
                arcObj.StartAngle = arc.StartAngleValue * 180 / Math.PI;
                arcObj.EndAngle = arc.EndAngleValue * 180 / Math.PI;
                break;
        }
    }

    public ObjectId? GetObjectId(Entity entity)
    {
        return _entityToObjectId.TryGetValue(entity, out var id) ? id : null;
    }

    public Entity? FindByObjectId(ObjectId objectId)
    {
        return _objectIdToEntity.TryGetValue(objectId, out var entity) ? entity : null;
    }
}

#region AutoCAD Version-Specific Converters

public class AutoCAD2021Converter : AutoCADConverterBase
{
    protected override LineEntity ConvertLineToInternal(object line)
    {
        var acadLine = line as Line;
        var start = new Vec2(acadLine!.StartPoint.X, acadLine.StartPoint.Y);
        var end = new Vec2(acadLine.EndPoint.X, acadLine.EndPoint.Y);
        return new LineEntity(start, end);
    }

    protected override CircleEntity ConvertCircleToInternal(object circle)
    {
        var acadCircle = circle as Circle;
        var center = new Vec2(acadCircle!.Center.X, acadCircle.Center.Y);
        return new CircleEntity(center, acadCircle.Radius);
    }

    protected override ArcEntity ConvertArcToInternal(object arc)
    {
        var acadArc = arc as Arc;
        var center = new Vec2(acadArc!.Center.X, acadArc.Center.Y);
        var startAngle = acadArc.StartAngle * Math.PI / 180.0;
        var endAngle = acadArc.EndAngle * Math.PI / 180.0;
        return new ArcEntity(center, acadArc.Radius, startAngle, endAngle);
    }

    protected override Entity? ConvertEllipseToInternal(object ellipse)
    {
        // TODO: Implement ellipse conversion
        return null;
    }

    protected override object ConvertLineToAutoCAD(LineEntity line)
    {
        var acadLine = new Line(
            new Point3d(line.StartPosition.X, line.StartPosition.Y, 0),
            new Point3d(line.EndPosition.X, line.EndPosition.Y, 0));
        return acadLine;
    }

    protected override object ConvertCircleToAutoCAD(CircleEntity circle)
    {
        var acadCircle = new Circle(
            new Point3d(circle.CenterPosition.X, circle.CenterPosition.Y, 0),
            Vector3d.ZAxis,
            circle.RadiusValue);
        return acadCircle;
    }

    protected override object ConvertArcToAutoCAD(ArcEntity arc)
    {
        var acadArc = new Arc(
            new Point3d(arc.CenterPosition.X, arc.CenterPosition.Y, 0),
            arc.RadiusValue,
            arc.StartAngleValue,
            arc.EndAngleValue);
        return acadArc;
    }
}

public class AutoCAD2022Converter : AutoCAD2021Converter { }
public class AutoCAD2023Converter : AutoCAD2021Converter { }
public class AutoCAD2024Converter : AutoCAD2021Converter { }
public class AutoCAD2025Converter : AutoCAD2021Converter { }
public class AutoCAD2026Converter : AutoCAD2021Converter { }

#endregion
