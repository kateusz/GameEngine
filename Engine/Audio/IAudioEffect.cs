using Audio;

namespace Engine.Audio;

internal interface IAudioEffect : IDisposable
{
    AudioEffectType Type { get; }
    uint SlotId { get; }
    void Apply(float amount);
}
