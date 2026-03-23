using System.Text.Json;
using Engine.Audio;
using Engine.Scene.Components;
using Shouldly;

namespace Engine.Tests.Audio;

public class AudioEffectConfigTests
{
    [Fact]
    public void AudioEffectConfig_Serializes_RoundTrip()
    {
        var config = new AudioEffectConfig
        {
            Type = AudioEffectType.Reverb,
            Enabled = true,
            Amount = 0.7f
        };

        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<AudioEffectConfig>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Type.ShouldBe(AudioEffectType.Reverb);
        deserialized.Enabled.ShouldBeTrue();
        deserialized.Amount.ShouldBe(0.7f);
    }

    [Fact]
    public void AudioEffectConfig_DefaultValues_AreCorrect()
    {
        var config = new AudioEffectConfig();

        config.Type.ShouldBe(AudioEffectType.Reverb);
        config.Enabled.ShouldBeTrue();
        config.Amount.ShouldBe(0.5f);
    }

    [Fact]
    public void AudioSourceComponent_Clone_DeepCopiesEffects()
    {
        var original = new AudioSourceComponent
        {
            Volume = 0.8f,
            Effects =
            [
                new AudioEffectConfig { Type = AudioEffectType.Reverb, Amount = 0.7f },
                new AudioEffectConfig { Type = AudioEffectType.LowPass, Amount = 0.3f }
            ]
        };

        var clone = (AudioSourceComponent)original.Clone();

        // Modify clone's effects
        clone.Effects[0].Amount = 0.1f;
        clone.Effects.RemoveAt(1);

        // Original should be unchanged
        clone.Effects[0].Amount.ShouldBe(0.1f);
        clone.Effects.Count.ShouldBe(1);
        original.Effects[0].Amount.ShouldBe(0.7f);
        original.Effects.Count.ShouldBe(2);
    }
}
