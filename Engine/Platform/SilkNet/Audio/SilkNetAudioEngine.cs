using Engine.Audio;
using Silk.NET.OpenAL;

namespace Engine.Platform.SilkNet.Audio;

public class AudioEngine
{
    private static IAudioEngine? _instance;
    public static IAudioEngine Instance => _instance ??= new SilkNetAudioEngine();
}

public unsafe class SilkNetAudioEngine : IAudioEngine
{
    private readonly Dictionary<string, IAudioClip> _loadedClips = new();

    private AL _al;
    private ALContext _alc;
    private Device* _device;
    private Context* _context;
    private readonly List<SilkNetAudioSource> _activeSources = new();

    public void Initialize()
    {
        try
        {
            _alc = ALContext.GetApi(true);
            _al = AL.GetApi(true);

            // Otwórz domyślne urządzenie audio
            _device = _alc.OpenDevice("");
            if (_device == null)
                throw new InvalidOperationException("Nie można otworzyć urządzenia audio");

            // Utwórz kontekst audio
            _context = _alc.CreateContext(_device, null);
            if (_context == null)
                throw new InvalidOperationException("Nie można utworzyć kontekstu audio");

            _alc.MakeContextCurrent(_context);

            // Ustaw podstawowe parametry
            _al.SetListenerProperty(ListenerFloat.Gain, 1.0f);
            _al.SetListenerProperty(ListenerVector3.Position, 0.0f, 0.0f, 0.0f);
            _al.SetListenerProperty(ListenerVector3.Velocity, 0.0f, 0.0f, 0.0f);

            // Ustaw orientację słuchacza (forward vector i up vector)
            var orientation = new[]
            {
                0.0f, 0.0f, -1.0f, // Forward
                0.0f, 1.0f, 0.0f
            }; // Up

            unsafe
            {
                fixed (float* ptr = orientation)
                {
                    _al.SetListenerProperty(ListenerFloatArray.Orientation, ptr);
                }
            }

            Console.WriteLine("SilkNet AudioEngine zainicjalizowany pomyślnie");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd inicjalizacji AudioEngine: {ex.Message}");
            throw;
        }
    }

    public void Shutdown()
    {
        try
        {
            // Zatrzymaj i usuń wszystkie aktywne źródła
            foreach (var source in _activeSources.ToArray())
            {
                source.Dispose();
            }

            _activeSources.Clear();

            // Zamknij kontekst i urządzenie
            if (_context != null)
            {
                _alc.MakeContextCurrent(null);
                _alc.DestroyContext(_context);
                _context = null;
            }

            if (_device != null)
            {
                _alc.CloseDevice(_device);
                _device = null;
            }

            _al?.Dispose();
            _alc?.Dispose();

            Console.WriteLine("SilkNet AudioEngine zamknięty");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd zamykania AudioEngine: {ex.Message}");
        }
    }

    public IAudioSource CreateAudioSource()
    {
        var source = new SilkNetAudioSource(_al);
        _activeSources.Add(source);
        return source;
    }

    private static IAudioClip CreateAudioClip(string path)
    {
        return new SilkNetAudioClip(path);
    }

    internal void UnregisterSource(SilkNetAudioSource source)
    {
        _activeSources.Remove(source);
    }

    public AL GetAL() => _al;

    // Virtual methods with default implementation that can be overridden
    public IAudioClip LoadAudioClip(string path)
    {
        if (_loadedClips.TryGetValue(path, out var existingClip))
            return existingClip;

        var clip = CreateAudioClip(path);
        clip.Load();
        _loadedClips[path] = clip;
        return clip;
    }

    public void UnloadAudioClip(string path)
    {
        if (_loadedClips.TryGetValue(path, out var clip))
        {
            clip.Unload();
            _loadedClips.Remove(path);
        }
    }

    public void PlayOneShot(string clipPath, float volume = 1.0f)
    {
        var source = CreateAudioSource();
        var clip = LoadAudioClip(clipPath);

        source.Clip = clip;
        source.Volume = volume;
        source.Play();

        // Automatyczne usuwanie źródła po zakończeniu odtwarzania
        Timer timer = null!;
        timer = new Timer(
            callback: _ => {
                source.Dispose();
                timer?.Dispose();
            },
            state: null,
            dueTime: TimeSpan.FromSeconds(clip.Duration),
            period: Timeout.InfiniteTimeSpan
        );
    }

    // Cleanup methods
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var clip in _loadedClips.Values)
            {
                clip.Unload();
            }

            _loadedClips.Clear();

            Shutdown();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Protected helper methods for subclasses
    protected void ClearLoadedClips()
    {
        foreach (var clip in _loadedClips.Values)
        {
            clip.Unload();
        }

        _loadedClips.Clear();
    }
}