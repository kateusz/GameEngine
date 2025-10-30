using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Scene.Serializer;

/// <summary>
/// Centralized serialization configuration for all serializers in the engine.
/// Provides consistent JSON serialization options across Scene, Prefab, and Animation systems.
/// </summary>
public static class SerializationConfig
{
    /// <summary>
    /// Default JSON serialization options used throughout the engine.
    /// Includes custom converters for Vector types and Rectangle.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new Vector2Converter(),
            new Vector3Converter(),
            new Vector4Converter(),
            new RectangleConverter(),
            new JsonStringEnumConverter()
        }
    };
}
