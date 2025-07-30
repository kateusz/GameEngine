using System.Text;
using Engine.Audio;

namespace Engine.Platform.SilkNet.Audio.Loaders;

public class WavLoader : IAudioLoader
    {
        public bool CanLoad(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() == ".wav";
        }

        public AudioData Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Plik WAV nie istnieje: {path}");

            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fileStream);

            // Sprawdź nagłówek RIFF
            var riffHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (riffHeader != "RIFF")
                throw new InvalidDataException("Nieprawidłowy nagłówek RIFF");

            var fileSize = reader.ReadUInt32();
            
            var waveHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (waveHeader != "WAVE")
                throw new InvalidDataException("Nieprawidłowy nagłówek WAVE");

            // Znajdź chunk fmt
            var fmtHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (fmtHeader != "fmt ")
                throw new InvalidDataException("Brak chunka fmt");

            var fmtSize = reader.ReadUInt32();
            var audioFormat = reader.ReadUInt16();
            var channels = reader.ReadUInt16();
            var sampleRate = reader.ReadUInt32();
            var byteRate = reader.ReadUInt32();
            var blockAlign = reader.ReadUInt16();
            var bitsPerSample = reader.ReadUInt16();

            // Sprawdź, czy format jest obsługiwany
            if (audioFormat != 1) // PCM
                throw new NotSupportedException($"Nieobsługiwany format audio WAV: {audioFormat}");

            if (bitsPerSample != 16 && bitsPerSample != 8 && bitsPerSample != 24)
                throw new NotSupportedException($"Nieobsługiwana głębia bitowa: {bitsPerSample}. Obsługiwane: 8, 16, 24 bit");

            // Pomiń ewentualne dodatkowe dane w chunk fmt
            if (fmtSize > 16)
            {
                reader.ReadBytes((int)(fmtSize - 16));
            }

            // Znajdź chunk data
            string dataHeader;
            uint dataSize;
            
            do
            {
                dataHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
                dataSize = reader.ReadUInt32();
                
                if (dataHeader != "data")
                {
                    // Pomiń ten chunk
                    reader.ReadBytes((int)dataSize);
                }
            } 
            while (dataHeader != "data" && fileStream.Position < fileStream.Length);

            if (dataHeader != "data")
                throw new InvalidDataException("Brak chunka data");

            // Wczytaj dane audio
            var audioData = reader.ReadBytes((int)dataSize);
            var originalBitsPerSample = bitsPerSample; // Zapisz oryginalną głębię bitową

            // Konwertuj do 16-bit jeśli potrzeba
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

            Console.WriteLine($"Załadowano WAV: {path}");
            Console.WriteLine($"  - Kanały: {channels}, Sample Rate: {sampleRate} Hz, Original: {originalBitsPerSample}bit -> 16bit");

            return new AudioData(audioData, (int)sampleRate, channels, bitsPerSample, AudioFormat.WAV);
        }

        private byte[] Convert8BitTo16Bit(byte[] data8Bit)
        {
            var data16Bit = new byte[data8Bit.Length * 2];
            
            for (int i = 0; i < data8Bit.Length; i++)
            {
                // Konwertuj 8-bit unsigned (0-255) do 16-bit signed (-32768 to 32767)
                short sample16 = (short)((data8Bit[i] - 128) * 256);
                
                // Little endian
                data16Bit[i * 2] = (byte)(sample16 & 0xFF);
                data16Bit[i * 2 + 1] = (byte)(sample16 >> 8);
            }
            
            return data16Bit;
        }

        private byte[] Convert24BitTo16Bit(byte[] data24Bit)
        {
            // 24-bit ma 3 bajty na próbkę, 16-bit ma 2 bajty na próbkę
            var samplesCount = data24Bit.Length / 3;
            var data16Bit = new byte[samplesCount * 2];
            
            for (int i = 0; i < samplesCount; i++)
            {
                int index24 = i * 3;
                int index16 = i * 2;
                
                // Wczytaj 24-bit sample (little endian: LSB, mid, MSB)
                int sample24 = data24Bit[index24] | 
                              (data24Bit[index24 + 1] << 8) | 
                              (data24Bit[index24 + 2] << 16);
                
                // Konwertuj z unsigned na signed (24-bit)
                if (sample24 > 0x7FFFFF) // jeśli bit znaku jest ustawiony
                {
                    sample24 = sample24 - 0x1000000; // odejmij 2^24 aby otrzymać wartość ujemną
                }
                
                // Konwertuj 24-bit (-8,388,608 to 8,388,607) do 16-bit (-32,768 to 32,767)
                // Dzielimy przez 256 (przesuwamy o 8 bitów w prawo)
                short sample16 = (short)(sample24 >> 8);
                
                // Zapisz jako little endian
                data16Bit[index16] = (byte)(sample16 & 0xFF);
                data16Bit[index16 + 1] = (byte)(sample16 >> 8);
            }
            
            return data16Bit;
        }
    }