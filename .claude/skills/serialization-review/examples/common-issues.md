# Common Serialization Issues

This document catalogs frequent serialization anti-patterns and their fixes.

## Anti-Pattern 1: Runtime Objects Not Marked [JsonIgnore]

**Symptom**: Scene files become huge (megabytes) or deserialization fails

**Why It's Bad**:
- Box2D `Body` objects contain complex internal state, pointers to physics world
- Not JSON-serializable (will throw exceptions or produce garbage data)
- Even if it worked, would create massive JSON files

**Fix**:
```csharp
// ✅ CORRECT
public class RigidBody2DComponent
{
    public BodyType Type { get; set; } = BodyType.Dynamic;
    public float Mass { get; set; } = 1.0f;

    [JsonIgnore]
    public Body? RuntimeBody { get; set; } // ✅ Excluded from serialization
}
```

---

## Anti-Pattern 2: Loaded Resources Instead of Paths

**Symptom**: Textures, meshes, or audio encoded as Base64 in scene files

**Example**:
```csharp
// ❌ WRONG
public class SpriteRendererComponent
{
    public Texture? Texture { get; set; } // ❌ Entire texture serialized
}
```

**JSON Output (BAD)**:
```json
{
  "Texture": {
    "Width": 512,
    "Height": 512,
    "Data": "iVBORw0KGgoAAAANSUhEUgAA..." // ❌ 100KB+ Base64 blob
  }
}
```

**Fix**:
```csharp
// ✅ CORRECT
public class SpriteRendererComponent
{
    // Serialize path
    public string TexturePath { get; set; } = string.Empty;

    // Runtime only
    [JsonIgnore]
    public Texture? LoadedTexture { get; set; }
}

// Load at runtime
public void LoadResources(ITextureFactory textureFactory)
{
    if (!string.IsNullOrEmpty(TexturePath))
    {
        LoadedTexture = textureFactory.LoadTexture(TexturePath);
    }
}
```

**JSON Output (GOOD)**:
```json
{
  "TexturePath": "Assets/Textures/sprite.png"
}
```

---

## Anti-Pattern 3: Missing Default Values for New Fields

**Symptom**: Old scenes crash or behave incorrectly when loaded in newer engine versions

**Fix**:
```csharp
// ✅ CORRECT - Sensible default
public class RigidBody2DComponent
{
    public float GravityScale { get; set; } = 1.0f; // ✅ Default value
}
```
---

## Anti-Pattern 4: Circular References in Entity Hierarchy

**Symptom**: StackOverflowException during serialization or infinite JSON output

**Fix**:
```csharp
// ✅ CORRECT - Store GUID, not object reference
public class Entity
{
    public Guid Id { get; set; }

    // Serialize GUID only
    public Guid? ParentId { get; set; }

    // Runtime reference (not serialized)
    [JsonIgnore]
    public Entity? Parent { get; set; }

    public List<Entity> Children { get; set; } = new();
}

// Resolve references after deserialization
public void ResolveHierarchy(Dictionary<Guid, Entity> entityMap)
{
    if (ParentId.HasValue && entityMap.TryGetValue(ParentId.Value, out var parent))
    {
        Parent = parent;
    }
}
```

---

## Anti-Pattern 5: Computed/Cached Values Serialized

**Symptom**: Wasted space in JSON, potential for stale data

**Fix**:
```csharp
// ✅ CORRECT
public class TransformComponent
{
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; }

    [JsonIgnore]
    public Matrix4x4 WorldMatrix => CalculateWorldMatrix(); // ✅ Computed on demand
}
```

**Rule**: Only serialize primary data. Derived/computed values should be [JsonIgnore].

---

## Anti-Pattern 6: Missing Error Handling in Resource Loading

**Symptom**: Null references or crashes when asset files are missing

**Example**:
```csharp
// ❌ WRONG - No error handling
public void LoadResources(ITextureFactory textureFactory)
{
    LoadedTexture = textureFactory.LoadTexture(TexturePath); // ❌ Throws if missing
}
```

**Fix**:
```csharp
// ✅ CORRECT - Graceful degradation
public void LoadResources(ITextureFactory textureFactory, ILogger logger)
{
    if (string.IsNullOrEmpty(TexturePath))
        return;

    try
    {
        LoadedTexture = textureFactory.LoadTexture(TexturePath);
    }
    catch (FileNotFoundException)
    {
        logger.Warn($"Missing texture: {TexturePath}");
        LoadedTexture = textureFactory.GetDefaultTexture(); // Fallback
    }
    catch (Exception ex)
    {
        logger.Error($"Failed to load texture {TexturePath}: {ex.Message}");
        LoadedTexture = textureFactory.GetDefaultTexture();
    }
}
```

---

## Anti-Pattern 7: Inconsistent Read/Write in Custom Converters

**Symptom**: Properties saved but not loaded (or vice versa)

**Example**:
```csharp
// ❌ WRONG - Asymmetric
public override void Write(Utf8JsonWriter writer, AnimationComponent value, ...)
{
    writer.WriteString("AssetPath", value.AssetPath);
    writer.WriteBoolean("IsPlaying", value.IsPlaying);
    writer.WriteNumber("Speed", value.PlaybackSpeed); // ❌ "Speed"
}

public override AnimationComponent Read(ref Utf8JsonReader reader, ...)
{
    case "AssetPath": /* ... */ break;
    case "IsPlaying": /* ... */ break;
    case "PlaybackSpeed": /* ... */ break; // ❌ Looking for "PlaybackSpeed"
}
// Result: PlaybackSpeed is saved but never loaded!
```

**Fix**:
```csharp
// ✅ CORRECT - Same property names
public override void Write(Utf8JsonWriter writer, AnimationComponent value, ...)
{
    writer.WriteString("AssetPath", value.AssetPath);
    writer.WriteBoolean("IsPlaying", value.IsPlaying);
    writer.WriteNumber("PlaybackSpeed", value.PlaybackSpeed); // ✅ "PlaybackSpeed"
}

public override AnimationComponent Read(ref Utf8JsonReader reader, ...)
{
    case "AssetPath": /* ... */ break;
    case "IsPlaying": /* ... */ break;
    case "PlaybackSpeed": /* ... */ break; // ✅ "PlaybackSpeed"
}
```

**Test Strategy**:
```csharp
// Always test round-trip
var original = new AnimationComponent { PlaybackSpeed = 2.5f };
var json = Serialize(original);
var deserialized = Deserialize(json);
Assert.AreEqual(original.PlaybackSpeed, deserialized.PlaybackSpeed);
```

---

## Anti-Pattern 8: Platform-Specific Path Separators

**Symptom**: Scene files break when moved between Windows/Linux/macOS

**Example**:
```csharp
// ❌ WRONG - Windows-specific
public string TexturePath { get; set; } = "Assets\\Textures\\sprite.png";
```

**JSON Output (BAD)**:
```json
{
  "TexturePath": "Assets\\Textures\\sprite.png"
}
// Breaks on Linux/macOS
```

**Fix**:
```csharp
// ✅ CORRECT - Use Path.Combine and forward slashes
public string TexturePath { get; set; } = "Assets/Textures/sprite.png";

// When constructing paths programmatically
var path = Path.Combine("Assets", "Textures", "sprite.png");
// Convert to forward slashes for storage
TexturePath = path.Replace(Path.DirectorySeparatorChar, '/');
```

---

## Anti-Pattern 9: No Null Checks After Deserialization

**Symptom**: NullReferenceException when accessing deserialized objects

**Example**:
```csharp
// ❌ WRONG
var scene = JsonSerializer.Deserialize<Scene>(json);
foreach (var entity in scene.Entities) // ❌ Crash if scene or Entities is null
{
    // ...
}
```

**Fix**:
```csharp
// ✅ CORRECT
var scene = JsonSerializer.Deserialize<Scene>(json);
if (scene == null)
{
    throw new InvalidDataException("Failed to deserialize scene");
}

foreach (var entity in scene.Entities ?? Enumerable.Empty<Entity>())
{
    // ...
}
```

---

## Detection Checklist

When reviewing code, search for these patterns:

```bash
# Find properties without [JsonIgnore] that should have it
grep -E "(Runtime|Loaded|Cached|Computed)" **/*.cs | grep "{ get; set; }"

# Find potential circular references
grep -E "public.*Parent.*\{ get; set; \}" **/*.cs

# Find potential platform-specific paths
grep '\\\\' **/*.scene

# Find large Base64 blobs in scene files
grep -o '"[A-Za-z0-9+/=]\{1000,\}"' **/*.scene
```

## Quick Reference Table

| Problem | Symptom | Fix |
|---------|---------|-----|
| Runtime objects serialized | Huge files, crashes | Add `[JsonIgnore]` |
| Loaded resources in JSON | Base64 blobs | Store paths, not objects |
| Missing default values | Old scenes break | Add default values to properties |
| Circular references | Stack overflow | Use GUIDs, resolve after load |
| Computed values saved | Wasted space | Mark computed properties `[JsonIgnore]` |
| No error handling | Crashes on missing assets | Try-catch with fallbacks |
| Asymmetric converter | Data loss | Match property names in Read/Write |
| Platform-specific paths | Cross-platform breaks | Use forward slashes, `Path.Combine` |
| No null checks | NullReferenceException | Check nulls after deserialization |
