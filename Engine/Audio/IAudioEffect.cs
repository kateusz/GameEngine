namespace Engine.Audio;

public interface IAudioEffect : IDisposable
{
    AudioEffectType Type { get; }
    uint SlotId { get; }
    void Apply(float amount);
}
