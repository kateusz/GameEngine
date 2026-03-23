namespace Engine.Audio;

internal sealed class NoOpAudioEffectFactory : IAudioEffectFactory
{
    public IAudioEffect CreateEffect(AudioEffectType type) => new NoOpAudioEffect(type);
}
