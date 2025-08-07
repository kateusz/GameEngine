using Engine.Platform.SilkNet.Audio;

namespace Sandbox;

public class Program
{
    private static SilkNetAudioEngine _audioEngine;
    
    public static void Main(string[] args)
    {
        try
        {
            //InitializeAudio();

            //TestBasicAudio();
            
            var app = new SandboxApplication();
            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Błąd aplikacji: {e.Message}");
        }
        finally
        {
            ShutdownAudio();
        }
    }

    private static void InitializeAudio()
    {
        Console.WriteLine("Inicjalizacja systemu audio...");

        _audioEngine = new SilkNetAudioEngine();
        _audioEngine.Initialize();

        Console.WriteLine("System audio zainicjalizowany pomyślnie!");
    }

    private static void ShutdownAudio()
    {
        Console.WriteLine("Zamykanie systemu audio...");

        _audioEngine?.Dispose();
        _audioEngine = null;

        Console.WriteLine("System audio zamknięty.");
    }
    
    private static void TestBasicAudio()
    {
        try
        {
            Console.WriteLine("Test podstawowych funkcji audio...");

            // Test 1: PlayOneShot
            Console.WriteLine("Test PlayOneShot...");
            AudioEngine.Instance.PlayOneShot("assets/audio/door.wav", 0.5f);

            Console.WriteLine("Czekam");
            Console.ReadLine();

            // Test 2: Kontrolowane odtwarzanie
            Console.WriteLine("Test kontrolowanego odtwarzania...");
            var audioSource = AudioEngine.Instance.CreateAudioSource();
            var audioClip = AudioEngine.Instance.LoadAudioClip("assets/audio/giant1.wav");

            audioSource.Clip = audioClip;
            audioSource.Volume = 0.3f;
            audioSource.Loop = true;
            audioSource.Play();

            // Poczekaj chwilę
            Thread.Sleep(2000);

            // Zatrzymaj
            audioSource.Stop();
            audioSource.Dispose();

            Console.WriteLine("Testy audio zakończone pomyślnie!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas testów audio: {ex.Message}");
        }
    }
}