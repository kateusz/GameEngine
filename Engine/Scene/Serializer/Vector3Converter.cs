using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Scene.Serializer;

public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read(); // StartObject
        reader.Read(); // PropertyName "X"
        var x = reader.GetSingle();
        reader.Read(); // PropertyName "Y"
        reader.Read(); // Value Y
        var y = reader.GetSingle();
        reader.Read(); // PropertyName "Z"
        reader.Read(); // Value Z
        var z = reader.GetSingle();
        reader.Read(); // EndObject

        return new Vector3(x, y, z);
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