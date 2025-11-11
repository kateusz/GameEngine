namespace Engine.Audio;

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

