using System.Text.Json.Serialization;

namespace Engine.Audio;

public interface IAudioClip
{
    string Path { get; }
    float Duration { get; }
    int SampleRate { get; }
    int Channels { get; }
    AudioFormat Format { get; }
    void Load();
    void Unload();
    bool IsLoaded { get; }
        
    [JsonIgnore]
    byte[] RawData { get; }
    int DataSize { get; }
}