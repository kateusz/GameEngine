using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Scene.Serializer;

public class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read(); // StartArray
            
        // Read each component (X, Y, Z) from the array
        var x = reader.GetSingle();
        reader.Read();
        var y = reader.GetSingle();
        reader.Read();

        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray(); // Start array
        writer.WriteNumberValue(value.X); // X as first element
        writer.WriteNumberValue(value.Y); // Y as second element
        writer.WriteEndArray(); // End array
    }
}