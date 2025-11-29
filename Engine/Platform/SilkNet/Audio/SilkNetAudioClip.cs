using Engine.Audio;
using Serilog;
using Silk.NET.OpenAL;

namespace Engine.Platform.SilkNet.Audio;

internal sealed class SilkNetAudioClip : IAudioClip, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<SilkNetAudioClip>();

    private readonly AL _al;
    private bool _disposed;

    public string Path { get; }
    public float Duration { get; private set; }
    public int SampleRate { get; private set; }
    public int Channels { get; private set; }
    public AudioFormat Format { get; private set; }
    public bool IsLoaded { get; private set; }
    public byte[] RawData { get; private set; }
    public int DataSize { get; private set; }

    internal uint BufferId { get; private set; }

    public SilkNetAudioClip(string path, AL al)
    {
        Path = path;
        _al = al;
        Format = AudioClipFactory.DetectFormat(path);

        if (!AudioClipFactory.IsSupportedFormat(path))
            throw new NotSupportedException($"Unsupported audio format: {path}");
    }

    public void Load()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SilkNetAudioClip));

        if (IsLoaded)
            return;

        try
        {
            LoadAudioFile();
            BufferId = _al.GenBuffer();

            var alFormat = GetOpenALFormat();
            unsafe
            {
                fixed (byte* ptr = RawData)
                {
                    _al.BufferData(BufferId, alFormat, ptr, RawData.Length, SampleRate);
                }
            }

            Duration = GetDuration();
            IsLoaded = true;
            Logger.Information("Loaded audio clip: {Path} ({Duration:F2}s)", Path, Duration);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error loading audio clip {Path}", Path);

            if (BufferId == 0)
                throw;

            try
            {
                _al.DeleteBuffer(BufferId);
            }
            catch (Exception deleteEx)
            {
                Logger.Warning(deleteEx, "Failed to delete buffer after load error for {Path}", Path);
            }
            finally
            {
                BufferId = 0;
            }

            throw;
        }
    }

    public void Unload()
    {
        // Unload becomes an alias for Dispose for backward compatibility
        Dispose();
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

    private float GetDuration()
    {
        var bytesPerSample = Channels * 2; // Assuming 16-bit
        return (float)DataSize / (SampleRate * bytesPerSample);
    }

    private void ClearLoadedClip()
    {
        RawData = null!;

        if (BufferId == 0) 
            return;
        
        try
        {
            _al.DeleteBuffer(BufferId);
            BufferId = 0;
            IsLoaded = false;
            Logger.Information("Unloaded audio clip: {Path}", Path);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error disposing audio clip {Path}", Path);
        }
    }

    public void Dispose()
    {
        if (_disposed) 
            return;
        
        _disposed = true;

        ClearLoadedClip();
    }
}