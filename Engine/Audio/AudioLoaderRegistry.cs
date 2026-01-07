using Engine.Platform.OpenAL.Loaders;

namespace Engine.Audio;

internal static class AudioLoaderRegistry
{
    private static readonly List<IAudioLoader> Loaders =
    [
        new WavLoader(),
        new OggLoader()
    ];

    public static void RegisterLoader(IAudioLoader loader)
    {
        if (loader == null)
            throw new ArgumentNullException(nameof(loader));
                
        Loaders.Add(loader);
    }

    public static void UnregisterLoader(IAudioLoader loader) => Loaders.Remove(loader);

    public static IAudioLoader? GetLoader(string path) => Loaders.FirstOrDefault(loader => loader.CanLoad(path));

    public static bool IsSupported(string path) => GetLoader(path) != null;

    public static AudioData LoadAudio(string path)
    {
        var loader = GetLoader(path);
        if (loader == null)
            throw new NotSupportedException($"Unsupported file format: {path}");

        return loader.Load(path);
    }
}