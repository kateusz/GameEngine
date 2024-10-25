using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Scene.Serializer
{
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Read(); // StartArray
            
            // Read each component (X, Y, Z) from the array
            var x = reader.GetSingle();
            reader.Read();
            var y = reader.GetSingle();
            reader.Read();
            var z = reader.GetSingle();
            reader.Read(); // EndArray

            return new Vector3(x, y, z);
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray(); // Start array
            writer.WriteNumberValue(value.X); // X as first element
            writer.WriteNumberValue(value.Y); // Y as second element
            writer.WriteNumberValue(value.Z); // Z as third element
            writer.WriteEndArray(); // End array
        }
    }
}