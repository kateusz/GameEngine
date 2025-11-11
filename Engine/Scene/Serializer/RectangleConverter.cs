using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Animation;

namespace Engine.Scene.Serializer;

public class RectangleConverter : JsonConverter<Rectangle>
{
    public override Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for Rectangle deserialization.");

        reader.Read();

        var x = reader.GetInt32();
        reader.Read();
        var y = reader.GetInt32();
        reader.Read();
        var width = reader.GetInt32();
        reader.Read();
        var height = reader.GetInt32();
        reader.Read();

        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for Rectangle deserialization.");

        return new Rectangle(x, y, width, height);
    }

    public override void Write(Utf8JsonWriter writer, Rectangle value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Width);
        writer.WriteNumberValue(value.Height);
        writer.WriteEndArray(); // End array
    }
}