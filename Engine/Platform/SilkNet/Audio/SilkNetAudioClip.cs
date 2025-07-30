using Engine.Audio;
using Silk.NET.OpenAL;

namespace Engine.Platform.SilkNet.Audio;

public class SilkNetAudioClip : IAudioClip
{
    private uint _bufferId;
    private bool _disposed = false;

    public string Path { get; }
    public float Duration { get; private set; }
    public int SampleRate { get; private set; }
    public int Channels { get; private set; }
    public AudioFormat Format { get; private set; }
    public bool IsLoaded { get; private set; }
    public byte[] RawData { get; private set; }
    public int DataSize { get; private set; }

    internal uint BufferId => _bufferId;

    public SilkNetAudioClip(string path)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Format = AudioClipFactory.DetectFormat(path);

        if (!AudioClipFactory.IsSupportedFormat(path))
            throw new NotSupportedException($"Nieobsługiwany format audio: {path}");
    }

    public void Load()
    {
        if (IsLoaded)
            return;

        try
        {
            // Ładowanie zostanie zaimplementowane w punkcie 3
            LoadAudioFile();

            // Utwórz bufor OpenAL
            var al = ((SilkNetAudioEngine)AudioEngine.Instance).GetAL();
            _bufferId = al.GenBuffer();

            // Określ format OpenAL na podstawie kanałów i głębi bitowej
            var alFormat = GetOpenALFormat();

            // Prześlij dane do bufora OpenAL
            al.BufferData(_bufferId, alFormat, RawData, SampleRate);

            // Oblicz czas trwania
            int bytesPerSample = Channels * 2; // Zakładając 16-bit
            Duration = (float)DataSize / (SampleRate * bytesPerSample);

            IsLoaded = true;
            Console.WriteLine($"Załadowano klip audio: {Path} ({Duration:F2}s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd ładowania klipu audio {Path}: {ex.Message}");
            throw;
        }
    }

    public void Unload()
    {
        if (!IsLoaded)
            return;

        try
        {
            if (_bufferId != 0)
            {
                var al = ((SilkNetAudioEngine)AudioEngine.Instance).GetAL();
                al.DeleteBuffer(_bufferId);
                _bufferId = 0;
            }

            RawData = null;
            DataSize = 0;
            IsLoaded = false;

            Console.WriteLine($"Zwolniono klip audio: {Path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd zwalniania klipu audio {Path}: {ex.Message}");
        }
    }

    private void LoadAudioFile()
    {
        try
        {
            if (!AudioLoaderRegistry.IsSupported(Path))
                throw new NotSupportedException($"Nieobsługiwany format pliku: {Path}");

            var audioData = AudioLoaderRegistry.LoadAudio(Path);
        
            RawData = audioData.Data;
            DataSize = audioData.Data.Length;
            SampleRate = audioData.SampleRate;
            Channels = audioData.Channels;
            Format = audioData.Format;
        
            Console.WriteLine($"Załadowano dane audio: {Path}");
            Console.WriteLine($"  - Sample Rate: {SampleRate} Hz");
            Console.WriteLine($"  - Kanały: {Channels}");
            Console.WriteLine($"  - Rozmiar: {DataSize} bajtów");
            Console.WriteLine($"  - Format: {Format}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd ładowania pliku audio {Path}: {ex.Message}");
            throw;
        }
    }

    private BufferFormat GetOpenALFormat()
    {
        // Zakładając 16-bit samples
        return Channels switch
        {
            1 => BufferFormat.Mono16,
            2 => BufferFormat.Stereo16,
            _ => throw new NotSupportedException($"Nieobsługiwana liczba kanałów: {Channels}")
        };
    }
    
    

    ~SilkNetAudioClip()
    {
        if (!_disposed && IsLoaded)
        {
            Console.WriteLine($"Uwaga: AudioClip {Path} nie został prawidłowo zwolniony. Wywołaj Unload().");
        }
    }
}