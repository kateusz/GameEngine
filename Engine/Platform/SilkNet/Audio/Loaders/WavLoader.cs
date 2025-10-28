using System.Text;
using Engine.Audio;
using Serilog;

namespace Engine.Platform.SilkNet.Audio.Loaders;

public class WavLoader : IAudioLoader
{
    private static readonly ILogger Logger = Log.ForContext<WavLoader>();
    
    public bool CanLoad(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() == ".wav";
        }

        public AudioData Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"WAV file does not exist: {path}");

            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fileStream);

            // Check RIFF header
            var riffHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (riffHeader != "RIFF")
                throw new InvalidDataException("Invalid RIFF header");

            var fileSize = reader.ReadUInt32();

            var waveHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (waveHeader != "WAVE")
                throw new InvalidDataException("Invalid WAVE header");

            // Find fmt chunk
            var fmtHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (fmtHeader != "fmt ")
                throw new InvalidDataException("Missing fmt chunk");

            var fmtSize = reader.ReadUInt32();
            var audioFormat = reader.ReadUInt16();
            var channels = reader.ReadUInt16();
            var sampleRate = reader.ReadUInt32();
            var byteRate = reader.ReadUInt32();
            var blockAlign = reader.ReadUInt16();
            var bitsPerSample = reader.ReadUInt16();

            // Check if format is supported
            if (audioFormat != 1) // PCM
                throw new NotSupportedException($"Unsupported WAV audio format: {audioFormat}");

            if (bitsPerSample != 16 && bitsPerSample != 8 && bitsPerSample != 24)
                throw new NotSupportedException($"Unsupported bit depth: {bitsPerSample}. Supported: 8, 16, 24 bit");

            // Skip any additional data in fmt chunk
            if (fmtSize > 16)
            {
                reader.ReadBytes((int)(fmtSize - 16));
            }

            // Find data chunk
            string dataHeader;
            uint dataSize;
            
            do
            {
                dataHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
                dataSize = reader.ReadUInt32();

                if (dataHeader != "data")
                {
                    // Skip this chunk
                    reader.ReadBytes((int)dataSize);
                }
            }
            while (dataHeader != "data" && fileStream.Position < fileStream.Length);

            if (dataHeader != "data")
                throw new InvalidDataException("Missing data chunk");

            // Read audio data
            var audioData = reader.ReadBytes((int)dataSize);
            var originalBitsPerSample = bitsPerSample; // Save original bit depth

            // Convert to 16-bit if needed
            if (bitsPerSample == 8)
            {
                audioData = Convert8BitTo16Bit(audioData);
                bitsPerSample = 16;
            }
            else if (bitsPerSample == 24)
            {
                audioData = Convert24BitTo16Bit(audioData);
                bitsPerSample = 16;
            }

            Logger.Debug("Loaded WAV: {Path}", path);
            Logger.Debug("  - Channels: {Channels}, Sample Rate: {SampleRate} Hz, Original: {OriginalBits}bit -> 16bit",
                channels, sampleRate, originalBitsPerSample);

            return new AudioData(audioData, (int)sampleRate, channels, bitsPerSample, AudioFormat.WAV);
        }

        private byte[] Convert8BitTo16Bit(byte[] data8Bit)
        {
            var data16Bit = new byte[data8Bit.Length * 2];

            for (int i = 0; i < data8Bit.Length; i++)
            {
                // Convert 8-bit unsigned (0-255) to 16-bit signed (-32768 to 32767)
                short sample16 = (short)((data8Bit[i] - 128) * 256);
                
                // Little endian
                data16Bit[i * 2] = (byte)(sample16 & 0xFF);
                data16Bit[i * 2 + 1] = (byte)(sample16 >> 8);
            }
            
            return data16Bit;
        }

        private byte[] Convert24BitTo16Bit(byte[] data24Bit)
        {
            // 24-bit has 3 bytes per sample, 16-bit has 2 bytes per sample
            var samplesCount = data24Bit.Length / 3;
            var data16Bit = new byte[samplesCount * 2];

            for (int i = 0; i < samplesCount; i++)
            {
                int index24 = i * 3;
                int index16 = i * 2;

                // Read 24-bit sample (little endian: LSB, mid, MSB)
                int sample24 = data24Bit[index24] |
                              (data24Bit[index24 + 1] << 8) |
                              (data24Bit[index24 + 2] << 16);

                // Convert from unsigned to signed (24-bit)
                if (sample24 > 0x7FFFFF) // if sign bit is set
                {
                    sample24 = sample24 - 0x1000000; // subtract 2^24 to get negative value
                }

                // Convert 24-bit (-8,388,608 to 8,388,607) to 16-bit (-32,768 to 32,767)
                // Divide by 256 (shift right by 8 bits)
                short sample16 = (short)(sample24 >> 8);

                // Save as little endian
                data16Bit[index16] = (byte)(sample16 & 0xFF);
                data16Bit[index16 + 1] = (byte)(sample16 >> 8);
            }

            return data16Bit;
        }
    }