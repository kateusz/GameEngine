using Engine.Platform.SilkNet.Audio.Loaders;

namespace Engine.Audio;

public static class AudioLoaderRegistry
{
    private static readonly List<IAudioLoader> _loaders = new()
    {
        new WavLoader(),
    };

    public static void RegisterLoader(IAudioLoader loader)
    {
        if (loader == null)
            throw new ArgumentNullException(nameof(loader));
                
        _loaders.Add(loader);
    }

    public static void UnregisterLoader(IAudioLoader loader)
    {
        _loaders.Remove(loader);
    }

    public static IAudioLoader GetLoader(string path)
    {
        return _loaders.FirstOrDefault(loader => loader.CanLoad(path));
    }

    public static bool IsSupported(string path)
    {
        return GetLoader(path) != null;
    }

    public static AudioData LoadAudio(string path)
    {
        var loader = GetLoader(path);
        if (loader == null)
            throw new NotSupportedException($"Nieobsługiwany format pliku: {path}");

        return loader.Load(path);
    }
}