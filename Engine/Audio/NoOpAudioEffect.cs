namespace Engine.Audio;

internal sealed class NoOpAudioEffect(AudioEffectType type) : IAudioEffect
{
    public AudioEffectType Type => type;
    public uint SlotId => 0;
    public void Apply(float amount) { }
    public void Dispose() { }
}
