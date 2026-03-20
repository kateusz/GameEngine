using System.Text.Json;
using Engine.Audio;

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

        Assert.NotNull(deserialized);
        Assert.Equal(AudioEffectType.Reverb, deserialized.Type);
        Assert.True(deserialized.Enabled);
        Assert.Equal(0.7f, deserialized.Amount);
    }

    [Fact]
    public void AudioEffectConfig_DefaultValues_AreCorrect()
    {
        var config = new AudioEffectConfig();

        Assert.True(config.Enabled);
        Assert.Equal(0.5f, config.Amount);
    }
}
