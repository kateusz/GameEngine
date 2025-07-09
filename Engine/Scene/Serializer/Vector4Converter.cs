using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Scene.Serializer;

public class Vector4Converter : JsonConverter<Vector4>
{
    public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read(); // StartArray
            
        // Read each component (X, Y, Z, W) from the array
        var x = reader.GetSingle();
        reader.Read();
        var y = reader.GetSingle();
        reader.Read();
        var z = reader.GetSingle();
        reader.Read();
        var w = reader.GetSingle();
        reader.Read(); // EndArray

        return new Vector4(x, y, z, w);
    }

    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray(); // Start array
        
        // Handle NaN and infinity values by replacing them with 0
        var x = float.IsNaN(value.X) || float.IsInfinity(value.X) ? 0f : value.X;
        var y = float.IsNaN(value.Y) || float.IsInfinity(value.Y) ? 0f : value.Y;
        var z = float.IsNaN(value.Z) || float.IsInfinity(value.Z) ? 0f : value.Z;
        var w = float.IsNaN(value.W) || float.IsInfinity(value.W) ? 0f : value.W;
        
        writer.WriteNumberValue(x); // X as first element
        writer.WriteNumberValue(y); // Y as second element
        writer.WriteNumberValue(z); // Z as third element
        writer.WriteNumberValue(w); // W as fourth element
        writer.WriteEndArray(); // End array
    }
}