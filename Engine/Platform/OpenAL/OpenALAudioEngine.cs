using System.Numerics;
using Engine.Audio;
using Serilog;
using Silk.NET.OpenAL;

namespace Engine.Platform.OpenAL;

internal sealed unsafe class OpenALAudioEngine(AL al, ALContext alc) : IAudioEngine
{
    private static readonly ILogger Logger = Log.ForContext<OpenALAudioEngine>();

    private readonly Dictionary<string, IAudioClip> _loadedClips = new();

    private Device* _device;
    private Context* _context;
    private readonly List<OpenALAudioSource> _activeSources = [];
    private bool _disposed;
    private bool _isAvailable;

    public void Initialize()
    {
        try
        {
            // Open default audio device
            _device = alc.OpenDevice("");
            if (_device == null)
            {
                Logger.Warning("Cannot open audio device - audio will be disabled. This is normal if no audio hardware is available.");
                _isAvailable = false;
                return;
            }

            // Create audio context
            _context = alc.CreateContext(_device, null);
            if (_context == null)
            {
                Logger.Warning("Cannot create audio context - audio will be disabled");
                alc.CloseDevice(_device);
                _device = null;
                _isAvailable = false;
                return;
            }

            alc.MakeContextCurrent(_context);

            // Set basic parameters
            al.SetListenerProperty(ListenerFloat.Gain, 1.0f);
            al.SetListenerProperty(ListenerVector3.Position, 0.0f, 0.0f, 0.0f);
            al.SetListenerProperty(ListenerVector3.Velocity, 0.0f, 0.0f, 0.0f);

            // Set listener orientation (forward vector and up vector)
            var orientation = new[]
            {
                0.0f, 0.0f, -1.0f, // Forward
                0.0f, 1.0f, 0.0f
            }; // Up

            fixed (float* ptr = orientation)
            {
                al.SetListenerProperty(ListenerFloatArray.Orientation, ptr);
            }

            _isAvailable = true;
            Logger.Information("SilkNet AudioEngine initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error initializing AudioEngine - audio will be disabled");
            _isAvailable = false;
        }
    }

    private void Shutdown()
    {
        if (!_isAvailable)
            return;

        try
        {
            // Stop and remove all active sources
            foreach (var source in _activeSources.ToArray())
            {
                source.Dispose();
            }

            _activeSources.Clear();

            // Close context and device
            if (_context != null)
            {
                alc.MakeContextCurrent(null);
                alc.DestroyContext(_context);
                _context = null;
            }

            if (_device != null)
            {
                alc.CloseDevice(_device);
                _device = null;
            }

            // al and alc are owned by the DI container — do not dispose them here

            Logger.Information("SilkNet AudioEngine shut down");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error shutting down AudioEngine");
        }
    }

    public IAudioSource CreateAudioSource()
    {
        if (!_isAvailable)
            return new NoOpAudioSource();

        var source = new OpenALAudioSource(al, UnregisterSource);
        _activeSources.Add(source);
        return source;
    }

    private IAudioClip CreateAudioClip(string path)
    {
        if (!_isAvailable)
            return new NoOpAudioClip();

        return new OpenALAudioClip(path, al);
    }

    private void UnregisterSource(OpenALAudioSource source) => _activeSources.Remove(source);
    
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
        if (!_isAvailable)
            return;

        var source = CreateAudioSource();
        var clip = LoadAudioClip(clipPath);

        source.Clip = clip;
        source.Volume = volume;
        source.Play();

        // Automatically remove source after playback finishes
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

    public void SetListenerPosition(Vector3 position)
    {
        if (!_isAvailable)
            return;

        al.SetListenerProperty(ListenerVector3.Position, position.X, position.Y, position.Z);
    }

    public void SetListenerOrientation(Vector3 forward, System.Numerics.Vector3 up)
    {
        if (!_isAvailable)
            return;

        var orientation = stackalloc float[6];
        orientation[0] = forward.X;
        orientation[1] = forward.Y;
        orientation[2] = forward.Z;
        orientation[3] = up.X;
        orientation[4] = up.Y;
        orientation[5] = up.Z;

        al.SetListenerProperty(ListenerFloatArray.Orientation, orientation);
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        ClearLoadedClips();
        Shutdown();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    // Protected helper methods for subclasses
    private void ClearLoadedClips()
    {
        foreach (var clip in _loadedClips.Values)
        {
            clip.Unload();
        }

        _loadedClips.Clear();
    }
}

internal sealed class NoOpAudioSource : IAudioSource
{
    private static readonly NoOpAudioClip DummyClip = new();

    public IAudioClip Clip { get; set; } = DummyClip;
    public float Volume { get; set; }
    public float Pitch { get; set; }
    public bool Loop { get; set; }
    public bool IsPlaying => false;
    public bool IsPaused => false;
    public float PlaybackPosition { get; set; }

    public void Play() { }
    public void Pause() { }
    public void Stop() { }
    public void SetPosition(Vector3 position) { }
    public void SetSpatialMode(bool is3D, float minDistance = 1.0f, float maxDistance = 100.0f) { }
    public void AddEffect(IAudioEffect effect) { }
    public void RemoveEffect(AudioEffectType type) { }
    public void ClearEffects() { }
    public bool HasEffect(AudioEffectType type) => false;
    public void UpdateEffect(AudioEffectType type, float amount) { }
    public IEnumerable<AudioEffectType> GetActiveEffectTypes() => [];
    public void Dispose() { }
}