using Xunit;
using AutoCADConstraintSolver.Entities;
using AutoCADConstraintSolver.Geometry;
using AutoCADConstraintSolver.Sketch;
using AutoCADConstraintSolver.Constraints;
using AutoCADConstraintSolver.IO;
using System.Text.Json;

namespace AutoCADConstraintSolver.Tests;

public class SerializationTests
{
    #region Basic Serialization Tests

    [Fact]
    public void SketchData_Serialization_RoundTrip()
    {
        // Create a sketch with entities
        var sketch = new Sketch();
        var p1 = new PointEntity(0, 0);
        var p2 = new PointEntity(100, 50);
        var line = new LineEntity(p1, p2);
        
        sketch.AddEntity(line);

        // Serialize
        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();

        // Verify JSON is valid
        Assert.NotNull(json);
        Assert.Contains("LineEntity", json);
        Assert.Contains("PointEntity", json);

        // Deserialize to new sketch
        var newSketch = new Sketch();
        var newSerializer = new SketchSerializer(newSketch);
        newSerializer.Deserialize(json);

        // Verify entities
        Assert.Equal(3, newSketch.Entities.Count); // 2 points + 1 line
    }

    [Fact]
    public void SketchSerializer_PreservesEntityTypes()
    {
        var sketch = new Sketch();
        
        // Add various entity types
        var point = new PointEntity(10, 20);
        var line = new LineEntity(new Vec2(0, 0), new Vec2(50, 50));
        var circle = new CircleEntity(new Vec2(100, 100), 25);
        var arc = new ArcEntity(new Vec2(200, 200), 30, 0, Math.PI);
        var ellipse = new EllipseEntity(new Vec2(300, 300), 40, 20, Math.PI / 4);
        
        sketch.AddEntity(point);
        sketch.AddEntity(line);
        sketch.AddEntity(circle);
        sketch.AddEntity(arc);
        sketch.AddEntity(ellipse);

        // Serialize and deserialize
        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();
        
        var newSketch = new Sketch();
        var newSerializer = new SketchSerializer(newSketch);
        newSerializer.Deserialize(json);

        // Verify all entities are preserved
        Assert.Equal(5, newSketch.Entities.Count);
    }

    [Fact]
    public void SketchSerializer_PreservesLineCoordinates()
    {
        var sketch = new Sketch();
        
        var p1 = new PointEntity(10, 20);
        var p2 = new PointEntity(100, 150);
        var line = new LineEntity(p1, p2);
        
        sketch.AddEntity(line);

        // Serialize
        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();
        
        // Deserialize
        var newSketch = new Sketch();
        var newSerializer = new SketchSerializer(newSketch);
        newSerializer.Deserialize(json);

        // Find the line in new sketch
        var newLine = newSketch.Entities.OfType<LineEntity>().FirstOrDefault();
        Assert.NotNull(newLine);
        
        // Verify coordinates
        Assert.Equal(10, newLine.Start.X.value, 1);
        Assert.Equal(20, newLine.Start.Y.value, 1);
        Assert.Equal(100, newLine.End.X.value, 1);
        Assert.Equal(150, newLine.End.Y.value, 1);
    }

    [Fact]
    public void SketchSerializer_PreservesCircleProperties()
    {
        var sketch = new Sketch();
        
        var center = new Vec2(150, 200);
        var radius = 50.0;
        var circle = new CircleEntity(center, radius);
        
        sketch.AddEntity(circle);

        // Serialize
        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();
        
        // Deserialize
        var newSketch = new Sketch();
        var newSerializer = new SketchSerializer(newSketch);
        newSerializer.Deserialize(json);

        // Find the circle
        var newCircle = newSketch.Entities.OfType<CircleEntity>().FirstOrDefault();
        Assert.NotNull(newCircle);
        
        // Verify properties
        Assert.Equal(150, newCircle.Center.X.value, 1);
        Assert.Equal(200, newCircle.Center.Y.value, 1);
        Assert.Equal(50, newCircle.Radius.value, 1);
    }

    #endregion

    #region Constraint Serialization Tests

    [Fact]
    public void SketchSerializer_PreservesConstraints()
    {
        var sketch = new Sketch();
        
        var p1 = new PointEntity(0, 0);
        var p2 = new PointEntity(100, 0);
        var line = new LineEntity(p1, p2);
        
        sketch.AddEntity(line);
        sketch.AddConstraint(new HorizontalConstraint(line));
        sketch.AddConstraint(new LengthConstraint(line, 100));

        // Serialize
        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();
        
        // Verify constraints are in JSON
        Assert.Contains("HorizontalConstraint", json);
        Assert.Contains("LengthConstraint", json);

        // Deserialize
        var newSketch = new Sketch();
        var newSerializer = new SketchSerializer(newSketch);
        newSerializer.Deserialize(json);

        // Verify constraints
        Assert.Equal(2, newSketch.Constraints.Count);
    }

    [Fact]
    public void SketchSerializer_RoundTrip_WithConstraints()
    {
        // Create a fully constrained rectangle
        var sketch = new Sketch();

        var p1 = new PointEntity(0, 0);
        var p2 = new PointEntity(100, 0);
        var p3 = new PointEntity(100, 60);
        var p4 = new PointEntity(0, 60);

        var l1 = new LineEntity(p1, p2);
        var l2 = new LineEntity(p2, p3);
        var l3 = new LineEntity(p3, p4);
        var l4 = new LineEntity(p4, p1);

        sketch.AddEntity(l1);
        sketch.AddEntity(l2);
        sketch.AddEntity(l3);
        sketch.AddEntity(l4);

        // Add constraints
        sketch.AddConstraint(new HorizontalConstraint(l1));
        sketch.AddConstraint(new HorizontalConstraint(l3));
        sketch.AddConstraint(new VerticalConstraint(l2));
        sketch.AddConstraint(new VerticalConstraint(l4));
        sketch.AddConstraint(new LengthConstraint(l1, 100));
        sketch.AddConstraint(new LengthConstraint(l2, 60));
        sketch.AddConstraint(new CoincidentConstraint(p1, p2));
        sketch.AddConstraint(new CoincidentConstraint(p2, p3));
        sketch.AddConstraint(new CoincidentConstraint(p3, p4));
        sketch.AddConstraint(new CoincidentConstraint(p4, p1));

        // Serialize
        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();
        
        // Deserialize
        var newSketch = new Sketch();
        var newSerializer = new SketchSerializer(newSketch);
        newSerializer.Deserialize(json);

        // Verify
        Assert.Equal(4, newSketch.Entities.Count);
        Assert.Equal(10, newSketch.Constraints.Count);

        // Solve and verify
        var result = newSketch.Solve();
        Assert.Equal(SolveResult.OKAY, result);
    }

    [Fact]
    public void SketchSerializer_PreservesConstraintValues()
    {
        var sketch = new Sketch();
        
        var p1 = new PointEntity(0, 0);
        var p2 = new PointEntity(100, 100);
        var line = new LineEntity(p1, p2);
        
        sketch.AddEntity(line);
        
        var lengthConstraint = new LengthConstraint(line, 150);
        sketch.AddConstraint(lengthConstraint);

        // Serialize
        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();

        // Deserialize
        var newSketch = new Sketch();
        var newSerializer = new SketchSerializer(newSketch);
        newSerializer.Deserialize(json);

        // Find length constraint
        var newLengthConstraint = newSketch.Constraints.OfType<LengthConstraint>().FirstOrDefault();
        Assert.NotNull(newLengthConstraint);
        Assert.Equal(150, newLengthConstraint.Length, 1);
    }

    #endregion

    #region JSON Format Tests

    [Fact]
    public void SketchSerializer_ValidJson_Format()
    {
        var sketch = new Sketch();
        var point = new PointEntity(10, 20);
        sketch.AddEntity(point);

        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();

        // Should not throw
        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);

        // Check structure
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("version", out _));
        Assert.True(root.TryGetProperty("entities", out _));
        Assert.True(root.TryGetProperty("constraints", out _));
    }

    [Fact]
    public void SketchSerializer_ContainsEntityData()
    {
        var sketch = new Sketch();
        var circle = new CircleEntity(new Vec2(100, 100), 50);
        sketch.AddEntity(circle);

        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();

        // Check entity data
        Assert.Contains("\"type\":\"CircleEntity\"", json);
        Assert.Contains("\"radius\":50", json);
    }

    [Fact]
    public void SketchSerializer_ContainsConstraintData()
    {
        var sketch = new Sketch();
        var p1 = new PointEntity(0, 0);
        var p2 = new PointEntity(100, 0);
        var line = new LineEntity(p1, p2);
        sketch.AddEntity(line);
        sketch.AddConstraint(new HorizontalConstraint(line));

        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();

        Assert.Contains("\"type\":\"HorizontalConstraint\"", json);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SketchSerializer_EmptySketch()
    {
        var sketch = new Sketch();

        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();

        // Empty sketch should serialize fine
        Assert.NotNull(json);
        Assert.Contains("\"entities\":[]", json);
        Assert.Contains("\"constraints\":[]", json);
    }

    [Fact]
    public void SketchSerializer_LargeSketch()
    {
        var sketch = new Sketch();

        // Create many entities
        for (int i = 0; i < 100; i++)
        {
            var point = new PointEntity(i * 10, i * 10);
            sketch.AddEntity(point);
        }

        var serializer = new SketchSerializer(sketch);
        var json = serializer.Serialize();

        // Should handle large sketches
        Assert.NotNull(json);
        Assert.Contains("\"entities\":", json);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void SaveSketch_CreatesFile()
    {
        var sketch = new Sketch();
        var line = new LineEntity(new Vec2(0, 0), new Vec2(100, 100));
        sketch.AddEntity(line);

        var tempPath = System.IO.Path.GetTempFileName();
        
        try
        {
            SketchSerializationHelper.SaveSketch(sketch, tempPath);
            
            // File should exist
            Assert.True(System.IO.File.Exists(tempPath));
            
            // File should have content
            var content = System.IO.File.ReadAllText(tempPath);
            Assert.NotEmpty(content);
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    [Fact]
    public void LoadSketch_ReadsFile()
    {
        var sketch = new Sketch();
        var circle = new CircleEntity(new Vec2(50, 50), 25);
        sketch.AddEntity(circle);
        sketch.AddConstraint(new RadiusConstraint(circle, 25));

        var tempPath = System.IO.Path.GetTempFileName();
        
        try
        {
            SketchSerializationHelper.SaveSketch(sketch, tempPath);
            
            // Load from file
            var loadedSketch = SketchSerializationHelper.LoadSketch(tempPath);
            
            // Verify
            Assert.Single(loadedSketch.Entities);
            Assert.Single(loadedSketch.Constraints);
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    [Fact]
    public void ExportToJson_ReturnsValidJson()
    {
        var sketch = new Sketch();
        var ellipse = new EllipseEntity(new Vec2(0, 0), 10, 5, Math.PI / 6);
        sketch.AddEntity(ellipse);

        var json = SketchSerializationHelper.ExportToJson(sketch);
        
        // Should be valid JSON
        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    [Fact]
    public void ImportFromJson_ModifiesExistingSketch()
    {
        var sketch = new Sketch();
        var originalPoint = new PointEntity(0, 0);
        sketch.AddEntity(originalPoint);

        // Create a different sketch
        var otherSketch = new Sketch();
        var line = new LineEntity(new Vec2(0, 0), new Vec2(50, 50));
        otherSketch.AddEntity(line);

        var json = SketchSerializationHelper.ExportToJson(otherSketch);

        // Import into existing sketch
        SketchSerializationHelper.ImportFromJson(sketch, json);

        // Should now have the imported entities
        Assert.Single(sketch.Entities);
        Assert.IsType<LineEntity>(sketch.Entities.First());
    }

    #endregion
}
