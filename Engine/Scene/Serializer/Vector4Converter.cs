using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Scene.Serializer
{
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
            writer.WriteNumberValue(value.X); // X as first element
            writer.WriteNumberValue(value.Y); // Y as second element
            writer.WriteNumberValue(value.Z); // Z as third element
            writer.WriteNumberValue(value.W); // W as fourth element
            writer.WriteEndArray(); // End array
        }
    }
}