using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Constraints;
using AutoCADConstraintSolver.Sketch;

namespace AutoCADConstraintSolver.IO;

/// <summary>
/// Sketch serialization data structure for JSON export/import
/// </summary>
public class SketchData
{
    /// <summary>Schema version for compatibility</summary>
    public string Version { get; set; } = "1.0";
    
    /// <summary>Entities in the sketch</summary>
    public List<EntityData> Entities { get; set; } = new();
    
    /// <summary>Constraints in the sketch</summary>
    public List<ConstraintData> Constraints { get; set; } = new();
    
    /// <summary>Entity ID mapping for references</summary>
    public Dictionary<string, int> EntityIds { get; set; } = new();
}

/// <summary>
/// Serialization data for entities
/// </summary>
public class EntityData
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double? X2 { get; set; }
    public double? Y2 { get; set; }
    public double? Radius { get; set; }
    public double? StartAngle { get; set; }
    public double? EndAngle { get; set; }
    public double? RadiusX { get; set; }
    public double? RadiusY { get; set; }
    public double? Rotation { get; set; }
    public int Index { get; set; }
}

/// <summary>
/// Serialization data for constraints
/// </summary>
public class ConstraintData
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public List<string> EntityIds { get; set; } = new();
    public double? Value { get; set; }
    public string? Option { get; set; }
}

/// <summary>
/// Sketch serializer for JSON export/import
/// Based on NoteCAD's incremental serialization approach
/// </summary>
public class SketchSerializer
{
    private readonly Sketch _sketch;
    private readonly Dictionary<Entity, string> _entityToId = new();
    private readonly Dictionary<string, Entity> _idToEntity = new();
    private int _nextEntityIndex = 0;

    public SketchSerializer(Sketch sketch)
    {
        _sketch = sketch;
    }

    /// <summary>
    /// Serialize sketch to JSON string
    /// </summary>
    public string Serialize()
    {
        BuildEntityMapping();
        
        var data = new SketchData
        {
            Version = "1.0",
            Entities = SerializeEntities(),
            Constraints = SerializeConstraints(),
            EntityIds = new Dictionary<string, int>(_entityToId.Count)
        };

        foreach (var kvp in _entityToId)
        {
            data.EntityIds[kvp.Value] = _idToEntity.Count;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(data, options);
    }

    /// <summary>
    /// Serialize sketch to file
    /// </summary>
    public void SerializeToFile(string filePath)
    {
        var json = Serialize();
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Deserialize sketch from JSON string
    /// </summary>
    public void Deserialize(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var data = JsonSerializer.Deserialize<SketchData>(json, options);
        if (data == null)
            throw new InvalidOperationException("Failed to deserialize sketch data");

        DeserializeEntities(data.Entities);
        DeserializeConstraints(data.Constraints);
    }

    /// <summary>
    /// Deserialize sketch from file
    /// </summary>
    public void DeserializeFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        Deserialize(json);
    }

    private void BuildEntityMapping()
    {
        _entityToId.Clear();
        _idToEntity.Clear();
        _nextEntityIndex = 0;

        foreach (var entity in _sketch.Entities)
        {
            var id = GenerateEntityId(entity);
            _entityToId[entity] = id;
            _idToEntity[id] = entity;
        }
    }

    private string GenerateEntityId(Entity entity)
    {
        var index = _nextEntityIndex++;
        return $"{entity.GetType().Name}_{index}";
    }

    private List<EntityData> SerializeEntities()
    {
        var entities = new List<EntityData>();
        var index = 0;

        foreach (var entity in _sketch.Entities)
        {
            var data = new EntityData
            {
                Id = _entityToId[entity],
                Type = entity.GetType().Name,
                Index = index++
            };

            switch (entity)
            {
                case PointEntity point:
                    data.X = point.X.value;
                    data.Y = point.Y.value;
                    break;

                case LineEntity line:
                    data.X = line.Start.X.value;
                    data.Y = line.Start.Y.value;
                    data.X2 = line.End.X.value;
                    data.Y2 = line.End.Y.value;
                    break;

                case CircleEntity circle:
                    data.X = circle.Center.X.value;
                    data.Y = circle.Center.Y.value;
                    data.Radius = circle.Radius.value;
                    break;

                case ArcEntity arc:
                    data.X = arc.Center.X.value;
                    data.Y = arc.Center.Y.value;
                    data.Radius = arc.Radius.value;
                    data.StartAngle = arc.StartAngle.value;
                    data.EndAngle = arc.EndAngle.value;
                    break;

                case EllipseEntity ellipse:
                    data.X = ellipse.Center.X.value;
                    data.Y = ellipse.Center.Y.value;
                    data.RadiusX = ellipse.RadiusX.value;
                    data.RadiusY = ellipse.RadiusY.value;
                    data.Rotation = ellipse.Rotation.value;
                    break;
            }

            entities.Add(data);
        }

        return entities;
    }

    private List<ConstraintData> SerializeConstraints()
    {
        var constraints = new List<ConstraintData>();

        foreach (var constraint in _sketch.Constraints)
        {
            var data = new ConstraintData
            {
                Id = constraint.Id.ToString(),
                Type = constraint.GetType().Name
            };

            foreach (var entity in constraint.GetEntities())
            {
                if (_entityToId.TryGetValue(entity, out var id))
                {
                    data.EntityIds.Add(id);
                }
            }

            // Capture constraint values
            if (constraint is PointsDistanceConstraint pd)
            {
                data.Value = pd.Distance;
            }
            else if (constraint is PointLineDistanceConstraint pld)
            {
                data.Value = pld.Distance;
            }
            else if (constraint is LengthConstraint lc)
            {
                data.Value = lc.Length;
            }
            else if (constraint is DiameterConstraint dc)
            {
                data.Value = dc.Diameter;
            }
            else if (constraint is AngleConstraint ac)
            {
                data.Value = ac.Angle * 180 / Math.PI; // Store in degrees
            }
            else if (constraint is RadiusConstraint rc)
            {
                data.Value = rc.Radius;
            }
            else if (constraint is FixationConstraint fc)
            {
                data.Value = 1; // Mark as fixed
            }
            else if (constraint is CirclesDistanceConstraint cd)
            {
                data.Value = cd.Distance;
                data.Option = cd.Option.ToString();
            }
            else if (constraint is LineCircleDistanceConstraint lcd)
            {
                data.Value = lcd.Distance;
                data.Option = lcd.Option.ToString();
            }

            constraints.Add(data);
        }

        return constraints;
    }

    private void DeserializeEntities(List<EntityData> entities)
    {
        _sketch.Clear();

        foreach (var data in entities)
        {
            Entity entity = data.Type switch
            {
                "PointEntity" => new PointEntity(data.X, data.Y),
                "LineEntity" => new LineEntity(new Vec2(data.X, data.Y), new Vec2(data.X2 ?? data.X, data.Y2 ?? data.Y)),
                "CircleEntity" => new CircleEntity(new Vec2(data.X, data.Y), data.Radius ?? 1),
                "ArcEntity" => new ArcEntity(
                    new Vec2(data.X, data.Y),
                    data.Radius ?? 1,
                    data.StartAngle ?? 0,
                    data.EndAngle ?? Math.PI),
                "EllipseEntity" => new EllipseEntity(
                    new Vec2(data.X, data.Y),
                    data.RadiusX ?? 1,
                    data.RadiusY ?? 1,
                    data.Rotation ?? 0),
                _ => throw new NotSupportedException($"Entity type {data.Type} is not supported")
            };

            _entityToId[data.Id] = entity;
            _idToEntity[data.Id] = entity;
            _sketch.AddEntity(entity);
        }
    }

    private void DeserializeConstraints(List<ConstraintData> constraints)
    {
        foreach (var data in constraints)
        {
            if (data.EntityIds.Count == 0) continue;

            Constraint constraint = data.Type switch
            {
                "CoincidentConstraint" => CreateCoincidentConstraint(data),
                "HorizontalConstraint" => CreateHorizontalConstraint(data),
                "VerticalConstraint" => CreateVerticalConstraint(data),
                "ParallelConstraint" => CreateParallelConstraint(data),
                "PerpendicularConstraint" => CreatePerpendicularConstraint(data),
                "TangentConstraint" => CreateTangentConstraint(data),
                "EqualLengthConstraint" => CreateEqualLengthConstraint(data),
                "FixationConstraint" => CreateFixationConstraint(data),
                "MidpointConstraint" => CreateMidpointConstraint(data),
                "ConcentricConstraint" => CreateConcentricConstraint(data),
                "EqualRadiusConstraint" => CreateEqualRadiusConstraint(data),
                "PointsDistanceConstraint" => CreatePointsDistanceConstraint(data),
                "PointLineDistanceConstraint" => CreatePointLineDistanceConstraint(data),
                "LengthConstraint" => CreateLengthConstraint(data),
                "DiameterConstraint" => CreateDiameterConstraint(data),
                "RadiusConstraint" => CreateRadiusConstraint(data),
                "AngleConstraint" => CreateAngleConstraint(data),
                "CirclesDistanceConstraint" => CreateCirclesDistanceConstraint(data),
                "LineCircleDistanceConstraint" => CreateLineCircleDistanceConstraint(data),
                _ => null
            };

            if (constraint != null)
            {
                _sketch.AddConstraint(constraint);
            }
        }
    }

    private Entity? GetEntity(string id)
    {
        return _idToEntity.TryGetValue(id, out var entity) ? entity : null;
    }

    private PointEntity? GetPointEntity(string id)
    {
        return GetEntity(id) as PointEntity;
    }

    private LineEntity? GetLineEntity(string id)
    {
        return GetEntity(id) as LineEntity;
    }

    private CircleEntity? GetCircleEntity(string id)
    {
        return GetEntity(id) as CircleEntity;
    }

    private ArcEntity? GetArcEntity(string id)
    {
        return GetEntity(id) as ArcEntity;
    }

    #region Constraint Creation Methods

    private CoincidentConstraint? CreateCoincidentConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2) return null;
        var p1 = GetPointEntity(data.EntityIds[0]);
        var p2 = GetPointEntity(data.EntityIds[1]);
        if (p1 != null && p2 != null)
            return new CoincidentConstraint(p1, p2);
        return null;
    }

    private HorizontalConstraint? CreateHorizontalConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 1) return null;
        var line = GetLineEntity(data.EntityIds[0]);
        return line != null ? new HorizontalConstraint(line) : null;
    }

    private VerticalConstraint? CreateVerticalConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 1) return null;
        var line = GetLineEntity(data.EntityIds[0]);
        return line != null ? new VerticalConstraint(line) : null;
    }

    private ParallelConstraint? CreateParallelConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2) return null;
        var line1 = GetLineEntity(data.EntityIds[0]);
        var line2 = GetLineEntity(data.EntityIds[1]);
        if (line1 != null && line2 != null)
            return new ParallelConstraint(line1, line2);
        return null;
    }

    private PerpendicularConstraint? CreatePerpendicularConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2) return null;
        var line1 = GetLineEntity(data.EntityIds[0]);
        var line2 = GetLineEntity(data.EntityIds[1]);
        if (line1 != null && line2 != null)
            return new PerpendicularConstraint(line1, line2);
        return null;
    }

    private TangentConstraint? CreateTangentConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2) return null;
        var line = GetLineEntity(data.EntityIds[0]);
        var circle = GetCircleEntity(data.EntityIds[1]) ?? GetArcEntity(data.EntityIds[1]);
        
        if (line != null && circle != null)
        {
            if (circle is CircleEntity c)
                return new TangentConstraint(line, c);
            if (circle is ArcEntity a)
                return new TangentConstraint(line, a);
        }
        return null;
    }

    private EqualLengthConstraint? CreateEqualLengthConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2) return null;
        var line1 = GetLineEntity(data.EntityIds[0]);
        var line2 = GetLineEntity(data.EntityIds[1]);
        if (line1 != null && line2 != null)
            return new EqualLengthConstraint(line1, line2);
        return null;
    }

    private FixationConstraint? CreateFixationConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 1) return null;
        var point = GetPointEntity(data.EntityIds[0]);
        if (point != null)
        {
            // Note: Position is already set from entity serialization
            return new FixationConstraint(point, point.Position);
        }
        return null;
    }

    private MidpointConstraint? CreateMidpointConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2) return null;
        var point = GetPointEntity(data.EntityIds[0]);
        var line = GetLineEntity(data.EntityIds[1]);
        if (point != null && line != null)
            return new MidpointConstraint(point, line);
        return null;
    }

    private ConcentricConstraint? CreateConcentricConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2) return null;
        var c1 = GetCircleEntity(data.EntityIds[0]) ?? GetArcEntity(data.EntityIds[0]);
        var c2 = GetCircleEntity(data.EntityIds[1]) ?? GetArcEntity(data.EntityIds[1]);
        
        if (c1 != null && c2 != null)
        {
            if (c1 is CircleEntity circle1 && c2 is CircleEntity circle2)
                return new ConcentricConstraint(circle1, circle2);
            if (c1 is ArcEntity arc1 && c2 is ArcEntity arc2)
                return new ConcentricConstraint(arc1, arc2);
            if (c1 is CircleEntity circle && c2 is ArcEntity arc)
                return new ConcentricConstraint(circle, arc);
        }
        return null;
    }

    private EqualRadiusConstraint? CreateEqualRadiusConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2) return null;
        var c1 = GetCircleEntity(data.EntityIds[0]) ?? GetArcEntity(data.EntityIds[0]);
        var c2 = GetCircleEntity(data.EntityIds[1]) ?? GetArcEntity(data.EntityIds[1]);
        
        if (c1 != null && c2 != null)
        {
            if (c1 is CircleEntity circle1 && c2 is CircleEntity circle2)
                return new EqualRadiusConstraint(circle1, circle2);
            if (c1 is ArcEntity arc1 && c2 is ArcEntity arc2)
                return new EqualRadiusConstraint(arc1, arc2);
            if (c1 is CircleEntity circle && c2 is ArcEntity arc)
                return new EqualRadiusConstraint(circle, arc);
        }
        return null;
    }

    private PointsDistanceConstraint? CreatePointsDistanceConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2 || !data.Value.HasValue) return null;
        var p1 = GetPointEntity(data.EntityIds[0]);
        var p2 = GetPointEntity(data.EntityIds[1]);
        if (p1 != null && p2 != null)
            return new PointsDistanceConstraint(p1, p2, data.Value.Value);
        return null;
    }

    private PointLineDistanceConstraint? CreatePointLineDistanceConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2 || !data.Value.HasValue) return null;
        var point = GetPointEntity(data.EntityIds[0]);
        var line = GetLineEntity(data.EntityIds[1]);
        if (point != null && line != null)
            return new PointLineDistanceConstraint(point, line, data.Value.Value);
        return null;
    }

    private LengthConstraint? CreateLengthConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 1 || !data.Value.HasValue) return null;
        var line = GetLineEntity(data.EntityIds[0]);
        return line != null ? new LengthConstraint(line, data.Value.Value) : null;
    }

    private DiameterConstraint? CreateDiameterConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 1 || !data.Value.HasValue) return null;
        var circle = GetCircleEntity(data.EntityIds[0]);
        return circle != null ? new DiameterConstraint(circle, data.Value.Value) : null;
    }

    private RadiusConstraint? CreateRadiusConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 1 || !data.Value.HasValue) return null;
        var circle = GetCircleEntity(data.EntityIds[0]) ?? GetArcEntity(data.EntityIds[0]);
        
        if (circle != null)
        {
            if (circle is CircleEntity c)
                return new RadiusConstraint(c, data.Value.Value);
            if (circle is ArcEntity a)
                return new RadiusConstraint(a, data.Value.Value);
        }
        return null;
    }

    private AngleConstraint? CreateAngleConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2 || !data.Value.HasValue) return null;
        var line1 = GetLineEntity(data.EntityIds[0]);
        var line2 = GetLineEntity(data.EntityIds[1]);
        if (line1 != null && line2 != null)
            return new AngleConstraint(line1, line2, data.Value.Value);
        return null;
    }

    private CirclesDistanceConstraint? CreateCirclesDistanceConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2 || !data.Value.HasValue) return null;
        
        var option = CirclesDistanceConstraint.DistanceOption.Outside;
        if (!string.IsNullOrEmpty(data.Option) && Enum.TryParse<CirclesDistanceConstraint.DistanceOption>(data.Option, out var parsed))
        {
            option = parsed;
        }

        var c1 = GetCircleEntity(data.EntityIds[0]) ?? GetArcEntity(data.EntityIds[0]);
        var c2 = GetCircleEntity(data.EntityIds[1]) ?? GetArcEntity(data.EntityIds[1]);
        
        if (c1 != null && c2 != null)
        {
            if (c1 is CircleEntity circle1 && c2 is CircleEntity circle2)
                return new CirclesDistanceConstraint(circle1, circle2, data.Value.Value, option);
            if (c1 is ArcEntity arc1 && c2 is ArcEntity arc2)
                return new CirclesDistanceConstraint(arc1, arc2, data.Value.Value, option);
            if (c1 is CircleEntity circle && c2 is ArcEntity arc)
                return new CirclesDistanceConstraint(circle, arc, data.Value.Value, option);
        }
        return null;
    }

    private LineCircleDistanceConstraint? CreateLineCircleDistanceConstraint(ConstraintData data)
    {
        if (data.EntityIds.Count < 2 || !data.Value.HasValue) return null;
        
        var option = LineCircleDistanceConstraint.LineCircleOption.Default;
        if (!string.IsNullOrEmpty(data.Option) && Enum.TryParse<LineCircleDistanceConstraint.LineCircleOption>(data.Option, out var parsed))
        {
            option = parsed;
        }

        var line = GetLineEntity(data.EntityIds[0]);
        var circle = GetCircleEntity(data.EntityIds[1]) ?? GetArcEntity(data.EntityIds[1]);
        
        if (line != null && circle != null)
        {
            if (circle is CircleEntity c)
                return new LineCircleDistanceConstraint(line, c, data.Value.Value, option);
            if (circle is ArcEntity a)
                return new LineCircleDistanceConstraint(line, a, data.Value.Value, option);
        }
        return null;
    }

    #endregion
}

/// <summary>
/// Helper class for sketch serialization operations
/// </summary>
public static class SketchSerializationHelper
{
    /// <summary>
    /// Save sketch to JSON file
    /// </summary>
    public static void SaveSketch(Sketch sketch, string filePath)
    {
        var serializer = new SketchSerializer(sketch);
        serializer.SerializeToFile(filePath);
    }

    /// <summary>
    /// Load sketch from JSON file
    /// </summary>
    public static Sketch LoadSketch(string filePath)
    {
        var sketch = new Sketch();
        var serializer = new SketchSerializer(sketch);
        serializer.DeserializeFromFile(filePath);
        return sketch;
    }

    /// <summary>
    /// Export sketch to JSON string
    /// </summary>
    public static string ExportToJson(Sketch sketch)
    {
        var serializer = new SketchSerializer(sketch);
        return serializer.Serialize();
    }

    /// <summary>
    /// Import sketch from JSON string
    /// </summary>
    public static void ImportFromJson(Sketch sketch, string json)
    {
        var serializer = new SketchSerializer(sketch);
        serializer.Deserialize(json);
    }
}
