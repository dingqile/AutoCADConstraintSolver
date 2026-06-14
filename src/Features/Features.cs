using System;
using System.Collections.Generic;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;

namespace AutoCADConstraintSolver.Features;

/// <summary>
/// 3D feature base class
/// </summary>
public abstract class Feature
{
    public string Name { get; set; } = "Feature";
    public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>
/// Extrusion feature - extends a 2D profile into 3D
/// </summary>
public class ExtrusionFeature : Feature
{
    /// <summary>
    /// Profile to extrude (collection of entities forming a closed loop)
    /// </summary>
    public List<Entity> Profile { get; } = new();
    
    /// <summary>
    /// Direction of extrusion
    /// </summary>
    public Vec3 Direction { get; set; } = Vec3.UnitZ;
    
    /// <summary>
    /// Distance to extrude
    /// </summary>
    public double Distance { get; set; } = 10;
    
    /// <summary>
    /// Whether to extrude in both directions
    /// </summary>
    public bool Symmetric { get; set; } = false;
    
    /// <summary>
    /// Draft angle for tapered extrusion (in degrees)
    /// </summary>
    public double DraftAngle { get; set; } = 0;
    
    /// <summary>
    /// Whether to create a solid (vs. surface)
    /// </summary>
    public bool Solid { get; set; } = true;
    
    /// <summary>
    /// Whether to union with existing geometry
    /// </summary>
    public bool BooleanUnion { get; set; } = false;

    public ExtrusionFeature()
    {
        Name = "Extrusion";
    }

    /// <summary>
    /// Create extrusion from profile
    /// </summary>
    public ExtrusionFeature(List<Entity> profile, double distance, Vec3? direction = null)
    {
        Profile = profile;
        Distance = distance;
        if (direction.HasValue)
            Direction = direction.Value;
        Name = "Extrusion";
    }

    /// <summary>
    /// Get the extrusion height in both directions
    /// </summary>
    public (double negative, double positive) GetHeights()
    {
        if (Symmetric)
        {
            return (-Distance / 2, Distance / 2);
        }
        return (0, Distance);
    }

    /// <summary>
    /// Check if the profile forms a valid closed loop
    /// </summary>
    public bool IsValidProfile()
    {
        if (Profile.Count < 3) return false;
        
        // Check for connectivity
        // In a real implementation, this would verify that entities form a closed loop
        return true;
    }
}

/// <summary>
/// Revolution feature - rotates a 2D profile around an axis
/// </summary>
public class RevolutionFeature : Feature
{
    /// <summary>
    /// Profile to revolve
    /// </summary>
    public List<Entity> Profile { get; } = new();
    
    /// <summary>
    /// Axis of revolution (start point)
    /// </summary>
    public Vec3 AxisPoint { get; set; } = Vec3.Zero;
    
    /// <summary>
    /// Direction of axis
    /// </summary>
    public Vec3 AxisDirection { get; set; } = Vec3.UnitZ;
    
    /// <summary>
    /// Start angle of revolution (degrees)
    /// </summary>
    public double StartAngle { get; set; } = 0;
    
    /// <summary>
    /// End angle of revolution (degrees)
    /// </summary>
    public double EndAngle { get; set; } = 360;
    
    /// <summary>
    /// Whether to create a solid
    /// </summary>
    public bool Solid { get; set; } = true;
    
    /// <summary>
    /// Whether to union with existing geometry
    /// </summary>
    public bool BooleanUnion { get; set; } = false;

    public RevolutionFeature()
    {
        Name = "Revolution";
    }

    /// <summary>
    /// Create revolution from profile
    /// </summary>
    public RevolutionFeature(List<Entity> profile, Vec3 axisPoint, Vec3 axisDirection, double endAngle = 360)
    {
        Profile = profile;
        AxisPoint = axisPoint;
        AxisDirection = axisDirection.Normalized();
        EndAngle = endAngle;
        Name = "Revolution";
    }

    /// <summary>
    /// Get the revolution angle in radians
    /// </summary>
    public double GetAngleRadians()
    {
        return (EndAngle - StartAngle) * Math.PI / 180.0;
    }

    /// <summary>
    /// Check if this is a full revolution
    /// </summary>
    public bool IsFullRevolution()
    {
        return Math.Abs(EndAngle - StartAngle - 360) < 1e-6;
    }
}

/// <summary>
/// 3D Box primitive
/// </summary>
public class BoxFeature : Feature
{
    public Vec3 Corner { get; set; } = Vec3.Zero;
    public double Width { get; set; } = 10;
    public double Height { get; set; } = 10;
    public double Depth { get; set; } = 10;

    public BoxFeature()
    {
        Name = "Box";
    }

    public BoxFeature(Vec3 corner, double width, double height, double depth)
    {
        Corner = corner;
        Width = width;
        Height = height;
        Depth = depth;
        Name = "Box";
    }

    public Vec3 Size => new Vec3(Width, Height, Depth);

    public Vec3 Center => Corner + Size / 2;
}

/// <summary>
/// 3D Cylinder primitive
/// </summary>
public class CylinderFeature : Feature
{
    public Vec3 BaseCenter { get; set; } = Vec3.Zero;
    public double Radius { get; set; } = 5;
    public double Height { get; set; } = 10;
    public Vec3 Direction { get; set; } = Vec3.UnitZ;

    public CylinderFeature()
    {
        Name = "Cylinder";
    }

    public CylinderFeature(Vec3 baseCenter, double radius, double height, Vec3? direction = null)
    {
        BaseCenter = baseCenter;
        Radius = radius;
        Height = height;
        if (direction.HasValue)
            Direction = direction.Value;
        Name = "Cylinder";
    }

    public Vec3 TopCenter => BaseCenter + Direction * Height;
}

/// <summary>
/// 3D Sphere primitive
/// </summary>
public class SphereFeature : Feature
{
    public Vec3 Center { get; set; } = Vec3.Zero;
    public double Radius { get; set; } = 5;

    public SphereFeature()
    {
        Name = "Sphere";
    }

    public SphereFeature(Vec3 center, double radius)
    {
        Center = center;
        Radius = radius;
        Name = "Sphere";
    }
}

/// <summary>
/// Boolean operation types
/// </summary>
public enum BooleanOperation
{
    None,
    Union,
    Subtract,
    Intersect
}

/// <summary>
/// Boolean feature - combines two solid bodies
/// </summary>
public class BooleanFeature : Feature
{
    public Guid Body1 { get; set; }
    public Guid Body2 { get; set; }
    public BooleanOperation Operation { get; set; } = BooleanOperation.Union;

    public BooleanFeature()
    {
        Name = "Boolean";
    }

    public BooleanFeature(Guid body1, Guid body2, BooleanOperation operation)
    {
        Body1 = body1;
        Body2 = body2;
        Operation = operation;
        Name = operation.ToString();
    }
}

/// <summary>
/// Fillet feature - rounds edges
/// </summary>
public class FilletFeature : Feature
{
    public List<Guid> Edges { get; } = new();
    public double Radius { get; set; } = 2;

    public FilletFeature()
    {
        Name = "Fillet";
    }

    public FilletFeature(double radius)
    {
        Radius = radius;
        Name = "Fillet";
    }
}

/// <summary>
/// Chamfer feature - bevels edges
/// </summary>
public class ChamferFeature : Feature
{
    public List<Guid> Edges { get; } = new();
    public double Distance { get; set; } = 2;
    public double Angle { get; set; } = 45; // degrees

    public ChamferFeature()
    {
        Name = "Chamfer";
    }

    public ChamferFeature(double distance)
    {
        Distance = distance;
        Name = "Chamfer";
    }
}

/// <summary>
/// Feature collection for a part
/// </summary>
public class PartFeatures
{
    public List<Feature> Features { get; } = new();

    public ExtrusionFeature AddExtrusion(List<Entity> profile, double distance, Vec3? direction = null)
    {
        var feature = new ExtrusionFeature(profile, distance, direction);
        Features.Add(feature);
        return feature;
    }

    public RevolutionFeature AddRevolution(List<Entity> profile, Vec3 axisPoint, Vec3 axisDirection, double endAngle = 360)
    {
        var feature = new RevolutionFeature(profile, axisPoint, axisDirection, endAngle);
        Features.Add(feature);
        return feature;
    }

    public BoxFeature AddBox(Vec3 corner, double width, double height, double depth)
    {
        var feature = new BoxFeature(corner, width, height, depth);
        Features.Add(feature);
        return feature;
    }

    public CylinderFeature AddCylinder(Vec3 baseCenter, double radius, double height)
    {
        var feature = new CylinderFeature(baseCenter, radius, height);
        Features.Add(feature);
        return feature;
    }

    public SphereFeature AddSphere(Vec3 center, double radius)
    {
        var feature = new SphereFeature(center, radius);
        Features.Add(feature);
        return feature;
    }
}