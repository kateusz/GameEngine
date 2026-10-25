using Audio;

namespace Hounds.Marches;

static class MarchAudio
{
    public static void Play(IAudio audio, string name) =>
        audio.PlayOneShot($"audio/{name}.wav", 0.85f);
}
