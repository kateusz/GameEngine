using Audio;

namespace Engine.Audio;

internal readonly record struct AudioData(
    byte[] Data,
    int SampleRate,
    int Channels,
    int BitsPerSample,
    AudioFormat Format
);
