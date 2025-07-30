using Engine.Audio;

namespace Engine.Platform.SilkNet.Audio.Loaders;

public interface IAudioLoader
{
    bool CanLoad(string path);
    AudioData Load(string path);
}

public struct AudioData
{
    public byte[] Data;
    public int SampleRate;
    public int Channels;
    public int BitsPerSample;
    public AudioFormat Format;
        
    public AudioData(byte[] data, int sampleRate, int channels, int bitsPerSample, AudioFormat format)
    {
        Data = data;
        SampleRate = sampleRate;
        Channels = channels;
        BitsPerSample = bitsPerSample;
        Format = format;
    }
}