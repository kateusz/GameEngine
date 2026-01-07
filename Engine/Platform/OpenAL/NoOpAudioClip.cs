using Engine.Audio;

namespace Engine.Platform.OpenAL;

internal sealed class NoOpAudioClip : IAudioClip
{
    private static readonly byte[] EmptyData = [];

    public string Path => string.Empty;
    public float Duration => 0f;
    public int SampleRate => 0;
    public int Channels => 0;
    public AudioFormat Format => AudioFormat.Unknown;
    public bool IsLoaded => false;
    public byte[] RawData => EmptyData;
    public int DataSize => 0;

    public void Load() { }
    public void Unload() { }
}