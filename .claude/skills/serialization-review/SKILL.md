---
name: serialization-review
description: Review JSON serialization implementation for scenes, prefabs, and components including custom JsonConverter usage, handling of circular references, resource path serialization, JsonIgnore attributes for runtime data, version compatibility, and SceneSerializer patterns. Use when adding serializable components, debugging save/load issues, or implementing new asset types.
---

# Serialization Review

## Overview
This skill audits JSON serialization implementation to ensure scenes, prefabs, and components serialize/deserialize correctly, maintain version compatibility, handle resource references properly, and follow established serialization patterns.

## When to Use
Invoke this skill when:
- Adding new serializable components
- Debugging scene save/load issues
- Implementing new asset types (prefabs, animations, tilemaps)
- Refactoring component data structures
- Versioning serialization format
- Questions about custom JsonConverter implementation
- Investigating data corruption or missing data after load

## Serialization Architecture

### SceneSerializer
**Location**: `Engine/Scene/Serializer/SceneSerializer.cs`

**Responsibilities**:
- Serialize entire scene to JSON
- Deserialize scene from JSON
- Handle entity hierarchy
- Manage component serialization
- Support custom converters

**Usage**:
```csharp
public class SceneSerializer
{
    public void Serialize(Scene scene, string path)
    {
        var json = JsonSerializer.Serialize(scene, _options);
        File.WriteAllText(path, json);
    }

    public Scene Deserialize(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Scene>(json, _options);
    }
}
```

### Custom Converters
**Location**: `Engine/Scene/Serializer/`

**Existing Converters**:
- `Vector2Converter`, `Vector3Converter`, `Vector4Converter`
- `QuaternionConverter`
- `ColorConverter`
- `TileMapComponentConverter`
- `AnimationComponentConverter`
- Component-specific converters for complex types

## Serialization Patterns

### 1. Standard Component Serialization

**Simple Component** (auto-serialization):
```csharp
public class TransformComponent
{
    public Vector3 Translation { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; } = Vector3.One;

    // Automatically serialized by System.Text.Json
    // No custom converter needed
}
```

**JSON Output**:
```json
{
  "Translation": { "X": 0, "Y": 0, "Z": 0 },
  "Rotation": { "X": 0, "Y": 0, "Z": 0 },
  "Scale": { "X": 1, "Y": 1, "Z": 1 }
}
```

### 2. JsonIgnore for Runtime Data

**Pattern**: Use `[JsonIgnore]` for runtime-only data

```csharp
public class RigidBody2DComponent
{
    // Serialized properties
    public BodyType Type { get; set; } = BodyType.Dynamic;
    public float Mass { get; set; } = 1.0f;
    public bool FixedRotation { get; set; } = false;

    // Runtime-only (not serialized)
    [JsonIgnore]
    public Body? RuntimeBody { get; set; }

    // Runtime-only (not serialized)
    [JsonIgnore]
    public World? RuntimeWorld { get; set; }
}
```

**Why?**: Runtime objects (physics bodies, loaded meshes, textures) should not be serialized - they're recreated at runtime.

### 3. Resource Path Serialization

**Pattern**: Store paths, not loaded resources

```csharp
public class SpriteRendererComponent
{
    // Serialize path, not loaded texture
    public string TexturePath { get; set; } = string.Empty;

    // Color is serialized
    public Vector4 Color { get; set; } = Vector4.One;

    // Runtime-only loaded texture (not serialized)
    [JsonIgnore]
    public Texture? LoadedTexture { get; set; }
}

// At runtime, load texture from path
if (!string.IsNullOrEmpty(sprite.TexturePath))
{
    sprite.LoadedTexture = textureFactory.LoadTexture(sprite.TexturePath);
}
```

**JSON Output**:
```json
{
  "TexturePath": "Assets/Textures/sprite.png",
  "Color": { "X": 1, "Y": 1, "Z": 1, "W": 1 }
}
```

### 4. Custom Converter for Complex Types

**When Needed**:
- Complex nested structures (TileMapComponent, AnimationComponent)
- Custom serialization format
- Backward compatibility requirements
- Optimized JSON format

**Example: AnimationComponent**:
```csharp
public class AnimationComponentConverter : JsonConverter<AnimationComponent>
{
    public override AnimationComponent Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var component = new AnimationComponent();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return component;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;
                reader.Read();

                switch (propertyName)
                {
                    case "AssetPath":
                        component.AssetPath = reader.GetString() ?? string.Empty;
                        break;
                    case "IsPlaying":
                        component.IsPlaying = reader.GetBoolean();
                        break;
                    case "CurrentClipIndex":
                        component.CurrentClipIndex = reader.GetInt32();
                        break;
                    // ... other properties
                }
            }
        }

        return component;
    }

    public override void Write(
        Utf8JsonWriter writer,
        AnimationComponent value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("AssetPath", value.AssetPath);
        writer.WriteBoolean("IsPlaying", value.IsPlaying);
        writer.WriteNumber("CurrentClipIndex", value.CurrentClipIndex);
        // ... other properties
        writer.WriteEndObject();
    }
}

// Register converter
var options = new JsonSerializerOptions();
options.Converters.Add(new AnimationComponentConverter());
```

### 5. Entity Hierarchy Serialization

**Pattern**: Serialize parent-child relationships

```csharp
public class Entity
{
    public Guid ID { get; set; }
    public string Name { get; set; }

    // Serialize parent ID (not object reference)
    public Guid? ParentID { get; set; }

    // Runtime-only (not serialized)
    [JsonIgnore]
    public Entity? Parent { get; set; }

    // Serialize child IDs
    public List<Guid> ChildIDs { get; set; } = new();

    // Runtime-only (not serialized)
    [JsonIgnore]
    public List<Entity> Children { get; set; } = new();

    // Components
    public Dictionary<Type, object> Components { get; set; } = new();
}
```

**Reconstruction After Load**:
```csharp
public void ReconstructHierarchy(Scene scene)
{
    foreach (var entity in scene.Entities)
    {
        // Rebuild parent reference
        if (entity.ParentID.HasValue)
        {
            entity.Parent = scene.GetEntityByID(entity.ParentID.Value);
        }

        // Rebuild children references
        foreach (var childID in entity.ChildIDs)
        {
            var child = scene.GetEntityByID(childID);
            if (child != null)
            {
                entity.Children.Add(child);
            }
        }
    }
}
```

### 6. Version Compatibility

**Pattern**: Include version number in serialized data

```csharp
public class Scene
{
    // Version for backward compatibility
    public int SerializationVersion { get; set; } = 1;

    public string Name { get; set; }
    public List<Entity> Entities { get; set; } = new();
}

// In deserializer
public Scene Deserialize(string path)
{
    var json = File.ReadAllText(path);
    var scene = JsonSerializer.Deserialize<Scene>(json, _options);

    // Handle old versions
    if (scene.SerializationVersion < 1)
    {
        MigrateFromVersion0(scene);
    }

    return scene;
}

private void MigrateFromVersion0(Scene scene)
{
    // Update old data structures to new format
    foreach (var entity in scene.Entities)
    {
        // Migration logic
    }
}
```

## Common Serialization Issues

### 1. Circular Reference Detection

**Problem**: Parent references child, child references parent → infinite loop

**❌ WRONG**:
```csharp
public class Entity
{
    public Entity? Parent { get; set; } // Serializes parent
    public List<Entity> Children { get; set; } // Serializes children
    // Circular reference: Entity → Parent → Children → Entity → ...
}
```

**✅ SOLUTION**: Use IDs, not references
```csharp
public class Entity
{
    public Guid? ParentID { get; set; } // Serialize ID only

    [JsonIgnore]
    public Entity? Parent { get; set; } // Runtime-only
}
```

### 2. Missing JsonIgnore Attributes

**Problem**: Runtime data serialized, causing errors on load

**❌ WRONG**:
```csharp
public class MeshComponent
{
    public string MeshPath { get; set; }
    public Mesh LoadedMesh { get; set; } // WRONG - Mesh can't serialize!
}
```

**✅ CORRECT**:
```csharp
public class MeshComponent
{
    public string MeshPath { get; set; }

    [JsonIgnore]
    public Mesh? LoadedMesh { get; set; } // Runtime-only
}
```

### 3. Type Preservation for Polymorphic Collections

**Problem**: Deserializing interface/base class loses concrete type

**❌ PROBLEM**:
```csharp
public class Entity
{
    // System.Text.Json doesn't preserve concrete types by default
    public List<object> Components { get; set; }
}
```

**✅ SOLUTION 1**: Use concrete types dictionary
```csharp
public class Entity
{
    public Dictionary<string, object> Components { get; set; } = new();
    // Key is component type name: "TransformComponent", "SpriteRendererComponent"
}
```

**✅ SOLUTION 2**: Custom converter with type discrimination
```csharp
public class ComponentConverter : JsonConverter<object>
{
    public override object Read(/* ... */)
    {
        // Read "Type" property to determine concrete type
        string typeName = reader.GetString();
        Type componentType = Type.GetType(typeName);
        return JsonSerializer.Deserialize(ref reader, componentType, options);
    }

    public override void Write(/* ... */)
    {
        // Write "Type" property with concrete type name
        writer.WriteString("Type", value.GetType().AssemblyQualifiedName);
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
```

### 4. Default Values Not Serializing

**Problem**: Default values omitted, properties not set on deserialize

**❌ POTENTIAL ISSUE**:
```csharp
// With options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
public class TransformComponent
{
    public Vector3 Scale { get; set; } = Vector3.One;
}

// If Scale is Vector3.One, it's not serialized!
// On deserialize, Scale will be Vector3.Zero (default struct value)
```

**✅ SOLUTION**:
```csharp
// Don't ignore default values for important properties
var options = new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    WriteIndented = true
};

// Or use nullable types with default handling
public class TransformComponent
{
    private Vector3? _scale;
    public Vector3 Scale
    {
        get => _scale ?? Vector3.One;
        set => _scale = value;
    }
}
```

### 5. File Path Serialization (Cross-Platform)

**Problem**: Absolute paths break on different machines

**❌ WRONG**:
```csharp
public class SpriteRendererComponent
{
    // Absolute path - breaks cross-platform
    public string TexturePath { get; set; } = "C:\\Users\\Me\\Project\\sprite.png";
}
```

**✅ CORRECT**:
```csharp
public class SpriteRendererComponent
{
    // Relative path from project root
    public string TexturePath { get; set; } = "Assets/Textures/sprite.png";
}

// At runtime, resolve relative to project
string fullPath = Path.Combine(projectRoot, sprite.TexturePath);
```

## Serialization Options Configuration

```csharp
public class SceneSerializer
{
    private readonly JsonSerializerOptions _options;

    public SceneSerializer()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,  // Human-readable JSON
            PropertyNamingPolicy = null,  // Keep PascalCase
            IncludeFields = false,  // Only serialize properties
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,  // Handle cycles
            Converters =
            {
                new Vector2Converter(),
                new Vector3Converter(),
                new Vector4Converter(),
                new QuaternionConverter(),
                new TileMapComponentConverter(),
                new AnimationComponentConverter(),
                // ... other custom converters
            }
        };
    }
}
```

## Testing Checklist

- [ ] Component serializes to JSON without errors
- [ ] Component deserializes from JSON correctly
- [ ] All properties restored with correct values
- [ ] Runtime-only properties marked with `[JsonIgnore]`
- [ ] Resource paths are relative, not absolute
- [ ] No circular references in object graph
- [ ] Version number included for future compatibility
- [ ] Custom converter handles all properties
- [ ] Entity hierarchy preserved (parent-child relationships)
- [ ] Scene save → load → save produces identical JSON (idempotent)

## Debugging Serialization Issues

### Enable Logging
```csharp
try
{
    var json = JsonSerializer.Serialize(scene, _options);
    File.WriteAllText(path, json);
    Logger.Info($"Serialized scene to {path}");
}
catch (JsonException ex)
{
    Logger.Error($"Serialization failed: {ex.Message}");
    Logger.Error($"Path: {ex.Path}");
    Logger.Error($"Line: {ex.LineNumber}");
}
```

### Validate JSON
```csharp
// Deserialize and re-serialize to validate
var scene = Deserialize(path);
var json1 = File.ReadAllText(path);
var json2 = JsonSerializer.Serialize(scene, _options);

if (json1 != json2)
{
    Logger.Warning("Serialization not idempotent!");
    // Compare to find differences
}
```

## Output Format

**Issue**: [Serialization problem]
**Location**: [Component or file path]
**Problem**: [Specific issue - missing data, error, corruption]
**Recommendation**: [Fix with code example]
**Priority**: [Critical/High/Medium/Low]

### Example Output
```
**Issue**: Runtime physics body being serialized
**Location**: Engine/Scene/Components/RigidBody2DComponent.cs:15
**Problem**: RuntimeBody property not marked [JsonIgnore], causing serialization errors
**Recommendation**:
public class RigidBody2DComponent
{
    // Serialized properties
    public BodyType Type { get; set; }
    public float Mass { get; set; }

    // Add [JsonIgnore] to runtime-only properties
    [JsonIgnore]
    public Body? RuntimeBody { get; set; }
}

**Priority**: High (causes save/load failures)
```

## Reference Documentation
- **SceneSerializer**: `Engine/Scene/Serializer/SceneSerializer.cs`
- **Custom Converters**: `Engine/Scene/Serializer/` directory
- **Component Examples**: All components in `Engine/Scene/Components/`
- **CLAUDE.md**: Serialization patterns and examples

## Integration with Agents
This skill works with both **game-engine-expert** (for component serialization) and **game-editor-architect** (for asset pipeline and project file formats).

## Tool Restrictions
None - this skill may read code, analyze JSON output, and suggest serialization improvements.
