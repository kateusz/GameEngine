using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Scene.Serializer;

internal sealed class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read(); // StartArray
            
        // Read each component (X, Y) from the array
        var x = reader.GetSingle();
        reader.Read();
        var y = reader.GetSingle();
        reader.Read();

        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray(); // Start array
        
        // Handle NaN and infinity values by replacing them with 0
        var x = float.IsNaN(value.X) || float.IsInfinity(value.X) ? 0f : value.X;
        var y = float.IsNaN(value.Y) || float.IsInfinity(value.Y) ? 0f : value.Y;
        
        writer.WriteNumberValue(x); // X as first element
        writer.WriteNumberValue(y); // Y as second element
        writer.WriteEndArray(); // End array
    }
}