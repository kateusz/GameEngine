using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Animation;

namespace Engine.Scene.Serializer;

/// <summary>
/// Custom JSON converter for AnimationClip to initialize cached Duration and FrameDuration values.
/// </summary>
public class AnimationClipConverter : JsonConverter<AnimationClip>
{
    public override AnimationClip Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var name = root.GetProperty("Name").GetString() ?? string.Empty;
        var fps = root.GetProperty("Fps").GetSingle();
        var loop = root.GetProperty("Loop").GetBoolean();
        
        var framesArray = root.GetProperty("Frames");
        var frames = JsonSerializer.Deserialize<AnimationFrame[]>(framesArray.GetRawText(), options) ?? [];

        // Calculate cached values
        var duration = frames.Length / fps;
        var frameDuration = 1.0f / fps;

        return new AnimationClip
        {
            Name = name,
            Fps = fps,
            Loop = loop,
            Frames = frames,
            Duration = duration,
            FrameDuration = frameDuration
        };
    }

    public override void Write(Utf8JsonWriter writer, AnimationClip value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        writer.WriteString("Name", value.Name);
        writer.WriteNumber("Fps", value.Fps);
        writer.WriteBoolean("Loop", value.Loop);
        
        writer.WritePropertyName("Frames");
        JsonSerializer.Serialize(writer, value.Frames, options);
        
        writer.WriteEndObject();
    }
}
