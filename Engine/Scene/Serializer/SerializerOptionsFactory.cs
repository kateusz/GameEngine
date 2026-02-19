using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Scene.Serializer;

internal static class SerializerOptionsFactory
{
    internal static readonly JsonSerializerOptions Default = new()
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
