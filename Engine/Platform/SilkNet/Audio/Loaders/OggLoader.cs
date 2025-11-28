using Engine.Audio;
using NVorbis;
using Serilog;

namespace Engine.Platform.SilkNet.Audio.Loaders;

/// <summary>
/// Audio loader for OGG Vorbis format files.
/// Decodes compressed OGG audio to 16-bit PCM for OpenAL playback.
/// </summary>
internal sealed class OggLoader : IAudioLoader
{
    private static readonly ILogger Logger = Log.ForContext<OggLoader>();

    /// <summary>
    /// Checks if this loader can handle the specified file.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <returns>True if the file has a .ogg extension.</returns>
    public bool CanLoad(string path)
    {
        return Path.GetExtension(path).Equals(".ogg", StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Loads and decodes an OGG Vorbis file to 16-bit PCM audio data.
    /// </summary>
    /// <param name="path">Path to the OGG file.</param>
    /// <returns>AudioData containing decoded PCM samples.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file is not a valid OGG Vorbis file.</exception>
    /// <exception cref="NotSupportedException">Thrown when the OGG file has unsupported characteristics.</exception>
    public AudioData Load(string path)
    {
        if (!File.Exists(path))
        {
            Logger.Error("OGG file not found: {Path}", path);
            throw new FileNotFoundException($"OGG file does not exist: {path}");
        }

        try
        {
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var vorbisReader = new VorbisReader(fileStream);

            // Extract metadata
            var sampleRate = vorbisReader.SampleRate;
            var channels = vorbisReader.Channels;
            var totalSamples = vorbisReader.TotalSamples;

            // Validate channel count
            if (channels > 2)
            {
                Logger.Error("Unsupported channel count in OGG file: {Channels} (max 2)", channels);
                throw new NotSupportedException($"OGG files with more than 2 channels are not supported. File has {channels} channels.");
            }

            if (totalSamples <= 0)
            {
                Logger.Error("Invalid OGG file: no samples detected in {Path}", path);
                throw new InvalidDataException($"OGG file contains no audio data: {path}");
            }

            Logger.Debug("Decoding OGG: {Path}", path);
            Logger.Debug("  - Sample Rate: {SampleRate} Hz", sampleRate);
            Logger.Debug("  - Channels: {Channels}", channels);
            Logger.Debug("  - Total Samples: {TotalSamples}", totalSamples);

            // Allocate buffer for float samples (interleaved: L, R, L, R, ...)
            var totalFloatSamples = (int)(totalSamples * channels);
            var floatBuffer = new float[totalFloatSamples];

            // Decode all samples from the OGG file
            var samplesRead = vorbisReader.ReadSamples(floatBuffer, 0, floatBuffer.Length);

            if (samplesRead == 0)
            {
                Logger.Error("Failed to read any samples from OGG file: {Path}", path);
                throw new InvalidDataException($"Could not read audio samples from OGG file: {path}");
            }

            Logger.Debug("  - Samples Read: {SamplesRead}", samplesRead);

            // Convert float32 samples to 16-bit PCM
            var pcm16Data = ConvertFloatToPCM16(floatBuffer, samplesRead);

            var durationSeconds = (double)totalSamples / sampleRate;
            Logger.Information("Loaded OGG: {Path} ({Duration:F2}s, {SampleRate}Hz, {Channels}ch)",
                path, durationSeconds, sampleRate, channels);

            return new AudioData(
                data: pcm16Data,
                sampleRate: sampleRate,
                channels: channels,
                bitsPerSample: 16,
                format: AudioFormat.OGG
            );
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (NotSupportedException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load OGG file: {Path}", path);
            throw new InvalidDataException($"Error loading OGG file: {path}. {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts float32 audio samples (range: -1.0 to 1.0) to 16-bit PCM (range: -32768 to 32767).
    /// </summary>
    /// <param name="floatSamples">Array of float samples from Vorbis decoder.</param>
    /// <param name="count">Number of samples to convert.</param>
    /// <returns>Byte array containing little-endian 16-bit PCM data.</returns>
    private static byte[] ConvertFloatToPCM16(float[] floatSamples, int count)
    {
        var pcm16Bytes = new byte[count * 2]; // 2 bytes per 16-bit sample

        for (var i = 0; i < count; i++)
        {
            // Clamp sample to valid range [-1.0, 1.0] to prevent clipping artifacts
            var sample = System.Math.Clamp(floatSamples[i], -1.0f, 1.0f);

            // Convert to 16-bit signed integer
            // Multiply by 32767 (not 32768) to avoid overflow at -1.0 * 32768 = -32768 (valid)
            var sample16 = (short)(sample * 32767.0f);

            // Write as little-endian bytes
            pcm16Bytes[i * 2] = (byte)(sample16 & 0xFF);         // LSB
            pcm16Bytes[i * 2 + 1] = (byte)((sample16 >> 8) & 0xFF); // MSB
        }

        return pcm16Bytes;
    }
}
