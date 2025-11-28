# Custom JsonConverter Examples

## When to Use Custom Converters

Create custom JsonConverters when you need:
- **Custom serialization format** - Optimize JSON structure or use specific format
- **Backward compatibility** - Handle breaking changes in component structure
- **Complex type handling** - Serialize types that don't work with default serialization

## Full Implementation Example: AnimationComponent

This example shows a complete custom converter for the AnimationComponent, including proper error handling and symmetric read/write operations.

```csharp
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class AnimationComponentConverter : JsonConverter<AnimationComponent>
{
    public override AnimationComponent Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var component = new AnimationComponent();

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

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
                    case "PlaybackSpeed":
                        component.PlaybackSpeed = (float)reader.GetDouble();
                        break;
                    case "Loop":
                        component.Loop = reader.GetBoolean();
                        break;
                    default:
                        // Skip unknown properties for forward compatibility
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of JSON");
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
        writer.WriteNumber("PlaybackSpeed", value.PlaybackSpeed);
        writer.WriteBoolean("Loop", value.Loop);

        writer.WriteEndObject();
    }
}
```

## Registering the Converter

Converters must be registered in the JsonSerializerOptions:

```csharp
public class SceneSerializer
{
    private readonly JsonSerializerOptions _options;

    public SceneSerializer()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Register custom converters
        _options.Converters.Add(new AnimationComponentConverter());
        _options.Converters.Add(new Vector2Converter());
        _options.Converters.Add(new Vector3Converter());
        _options.Converters.Add(new TileMapComponentConverter());
    }

    public void Serialize(Scene scene, string path)
    {
        var json = JsonSerializer.Serialize(scene, _options);
        File.WriteAllText(path, json);
    }

    public Scene Deserialize(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Scene>(json, _options)!;
    }
}
```

## Best Practices

### 1. Symmetric Read/Write
Ensure `Read()` and `Write()` methods handle the same properties in the same order:
```csharp
// Write these properties...
writer.WriteString("AssetPath", value.AssetPath);
writer.WriteBoolean("IsPlaying", value.IsPlaying);

// ...and Read them back
case "AssetPath":
    component.AssetPath = reader.GetString() ?? string.Empty;
    break;
case "IsPlaying":
    component.IsPlaying = reader.GetBoolean();
    break;
```

### 2. Error Handling
Add validation and error messages:
```csharp
if (reader.TokenType != JsonTokenType.StartObject)
{
    throw new JsonException($"Expected StartObject, got {reader.TokenType}");
}
```

### 3. Forward Compatibility
Use default case to skip unknown properties:
```csharp
default:
    // Skip unknown properties for forward compatibility
    reader.Skip();
    break;
```

This allows older versions of the engine to load scenes created with newer versions (within reason).

### 4. Null Coalescing
Always provide defaults for nullable types:
```csharp
component.AssetPath = reader.GetString() ?? string.Empty;
```

## Common Converter Patterns

### Vector Types
```csharp
public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        float x = 0, y = 0, z = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new Vector3(x, y, z);

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;
                reader.Read();

                switch (propertyName)
                {
                    case "X": x = (float)reader.GetDouble(); break;
                    case "Y": y = (float)reader.GetDouble(); break;
                    case "Z": z = (float)reader.GetDouble(); break;
                }
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteEndObject();
    }
}
```

### GUID Types
```csharp
public class GuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string guidString = reader.GetString() ?? string.Empty;
        return Guid.Parse(guidString);
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
```

## Testing Converters

Always test both serialization and deserialization:

```csharp
[Test]
public void AnimationComponent_SerializeDeserialize_PreservesData()
{
    // Arrange
    var original = new AnimationComponent
    {
        AssetPath = "Assets/Animations/walk.anim",
        IsPlaying = true,
        CurrentClipIndex = 2,
        PlaybackSpeed = 1.5f,
        Loop = true
    };

    var options = new JsonSerializerOptions();
    options.Converters.Add(new AnimationComponentConverter());

    // Act
    string json = JsonSerializer.Serialize(original, options);
    var deserialized = JsonSerializer.Deserialize<AnimationComponent>(json, options);

    // Assert
    Assert.AreEqual(original.AssetPath, deserialized.AssetPath);
    Assert.AreEqual(original.IsPlaying, deserialized.IsPlaying);
    Assert.AreEqual(original.CurrentClipIndex, deserialized.CurrentClipIndex);
    Assert.AreEqual(original.PlaybackSpeed, deserialized.PlaybackSpeed);
    Assert.AreEqual(original.Loop, deserialized.Loop);
}
```
