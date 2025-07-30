using Engine.Audio;
using Silk.NET.OpenAL;

namespace Engine.Platform.SilkNet.Audio;

public unsafe class SilkNetAudioEngine : AudioEngine
{
    private AL _al;
    private ALContext _alc;
    private Device* _device;
    private Context* _context;
    private readonly List<SilkNetAudioSource> _activeSources = new();

    public override void Initialize()
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

            // Ustaw podstawowe parametry słuchacza
            _al.SetListenerProperty(ListenerFloat.Gain, 1.0f);
            _al.SetListenerProperty(ListenerVector3.Position, 0.0f, 0.0f, 0.0f);
            _al.SetListenerProperty(ListenerVector3.Velocity, 0.0f, 0.0f, 0.0f);

            // Ustaw orientację słuchacza (forward vector i up vector)
            // Ustaw orientację słuchacza (forward vector i up vector)
            var orientation = new float[] { 0.0f, 0.0f, -1.0f,   // Forward
                0.0f, 1.0f,  0.0f }; // Up

            fixed (float* ptr = orientation)
            {
                _al.SetListenerProperty(ListenerFloatArray.Orientation, ptr);
            }


            Instance = this;

            Console.WriteLine("SilkNet AudioEngine zainicjalizowany pomyślnie");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd inicjalizacji AudioEngine: {ex.Message}");
            throw;
        }
    }

    public override void Shutdown()
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

    public override IAudioSource CreateAudioSource()
    {
        var source = new SilkNetAudioSource(_al);
        _activeSources.Add(source);
        return source;
    }

    protected override IAudioClip CreateAudioClip(string path)
    {
        return new SilkNetAudioClip(path);
    }

    internal void UnregisterSource(SilkNetAudioSource source)
    {
        _activeSources.Remove(source);
    }

    public AL GetAL() => _al;
}