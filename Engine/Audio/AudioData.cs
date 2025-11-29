namespace Engine.Audio;

public readonly record struct AudioData(
    byte[] Data,
    int SampleRate,
    int Channels,
    int BitsPerSample,
    AudioFormat Format
);
