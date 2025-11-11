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

        var x = value.X;
        var y = value.Y;
        var width = value.Width;
        var height = value.Height;

        writer.WriteNumberValue(x);
        writer.WriteNumberValue(y);
        writer.WriteNumberValue(width);
        writer.WriteNumberValue(height);
        writer.WriteEndArray(); // End array
    }
}