namespace Engine.Audio;

public static class AudioClipFactory
{
    public static AudioFormat DetectFormat(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
            
        return extension switch
        {
            ".wav" => AudioFormat.WAV,
            ".ogg" => AudioFormat.OGG,
            _ => AudioFormat.Unknown
        };
    }

    public static bool IsSupportedFormat(string path)
    {
        return DetectFormat(path) != AudioFormat.Unknown;
    }
}