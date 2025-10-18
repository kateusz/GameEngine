using Engine.Audio;
using Serilog;
using Silk.NET.OpenAL;

namespace Engine.Platform.SilkNet.Audio;

public class SilkNetAudioClip : IAudioClip
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<SilkNetAudioClip>();
    
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
            throw new NotSupportedException($"Unsupported audio format: {path}");
    }

    public void Load()
    {
        if (IsLoaded)
            return;

        try
        {
            // Load audio file
            LoadAudioFile();

            // Create OpenAL buffer
            var al = ((SilkNetAudioEngine)AudioEngine.Instance).GetAL();
            _bufferId = al.GenBuffer();

            // Determine OpenAL format based on channels and bit depth
            var alFormat = GetOpenALFormat();

            // Upload data to OpenAL buffer
            al.BufferData(_bufferId, alFormat, RawData, SampleRate);

            // Calculate duration
            int bytesPerSample = Channels * 2; // Assuming 16-bit
            Duration = (float)DataSize / (SampleRate * bytesPerSample);

            IsLoaded = true;
            Logger.Information("Loaded audio clip: {Path} ({Duration:F2}s)", Path, Duration);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error loading audio clip {Path}", Path);
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

            Logger.Information("Unloaded audio clip: {Path}", Path);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error unloading audio clip {Path}", Path);
        }
    }

    private void LoadAudioFile()
    {
        try
        {
            if (!AudioLoaderRegistry.IsSupported(Path))
                throw new NotSupportedException($"Unsupported file format: {Path}");

            var audioData = AudioLoaderRegistry.LoadAudio(Path);

            RawData = audioData.Data;
            DataSize = audioData.Data.Length;
            SampleRate = audioData.SampleRate;
            Channels = audioData.Channels;
            Format = audioData.Format;

            Logger.Debug("Loaded audio data: {Path}", Path);
            Logger.Debug("  - Sample Rate: {SampleRate} Hz", SampleRate);
            Logger.Debug("  - Channels: {Channels}", Channels);
            Logger.Debug("  - Size: {DataSize} bytes", DataSize);
            Logger.Debug("  - Format: {Format}", Format);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error loading audio file {Path}", Path);
            throw;
        }
    }

    private BufferFormat GetOpenALFormat()
    {
        // Assuming 16-bit samples
        return Channels switch
        {
            1 => BufferFormat.Mono16,
            2 => BufferFormat.Stereo16,
            _ => throw new NotSupportedException($"Unsupported number of channels: {Channels}")
        };
    }
    
    

    ~SilkNetAudioClip()
    {
        if (!_disposed && IsLoaded)
        {
            Logger.Warning("Warning: AudioClip {Path} was not properly unloaded. Call Unload().", Path);
        }
    }
}