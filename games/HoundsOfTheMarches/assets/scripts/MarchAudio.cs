using Audio;

namespace Hounds.Marches;

static class MarchAudio
{
    public static void Play(IAudio audio, string name)
    {
        try
        {
            audio.PlayOneShot($"audio/{name}.wav", 0.85f);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"audio hook '{name}' skipped: {ex.Message}");
        }
    }
}
