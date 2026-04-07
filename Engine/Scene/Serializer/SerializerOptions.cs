using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Scene.Serializer;

/// <summary>
/// Shared JSON serializer options for all scene/prefab/animation serializers.
/// Registered as a singleton via DI.
/// </summary>
internal sealed class SerializerOptions
{
    internal JsonSerializerOptions Options { get; }

    public SerializerOptions()
    {
        Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new Vector2Converter(),
                new Vector3Converter(),
                new Vector4Converter(),
                new JsonStringEnumConverter()
            }
        };
        Options.MakeReadOnly(populateMissingResolver: true);
    }
}
