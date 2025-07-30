namespace Engine.Audio;

public abstract class AudioEngine : IAudioEngine
{
    private static IAudioEngine _instance;
    private readonly Dictionary<string, IAudioClip> _loadedClips = new();
        
    public static IAudioEngine Instance 
    { 
        get 
        {
            if (_instance == null)
                throw new InvalidOperationException("AudioEngine nie został zainicjalizowany. Wywołaj Initialize() najpierw.");
            return _instance;
        }
        protected set => _instance = value;
    }

    public abstract void Initialize();
    public abstract void Shutdown();
    public abstract IAudioSource CreateAudioSource();
    protected abstract IAudioClip CreateAudioClip(string path);

    public virtual IAudioClip LoadAudioClip(string path)
    {
        if (_loadedClips.TryGetValue(path, out var existingClip))
            return existingClip;

        var clip = CreateAudioClip(path);
        clip.Load();
        _loadedClips[path] = clip;
        return clip;
    }

    public virtual void UnloadAudioClip(string path)
    {
        if (_loadedClips.TryGetValue(path, out var clip))
        {
            clip.Unload();
            _loadedClips.Remove(path);
        }
    }

    public virtual void PlayOneShot(string clipPath, float volume = 1.0f)
    {
        var source = CreateAudioSource();
        var clip = LoadAudioClip(clipPath);
            
        source.Clip = clip;
        source.Volume = volume;
        source.Play();
            
        // TODO: Dodać mechanizm automatycznego usuwania źródła po zakończeniu odtwarzania
    }

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
            _instance = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}