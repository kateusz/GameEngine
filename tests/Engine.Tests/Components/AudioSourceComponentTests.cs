using Engine.Audio;
using Engine.Scene.Components;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Components;

public class AudioSourceComponentTests
{
    [Fact]
    public void AudioSourceComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var component = new AudioSourceComponent();

        // Assert
        component.AudioClip.ShouldBeNull();
        component.Volume.ShouldBe(1.0f);
        component.Pitch.ShouldBe(1.0f);
        component.Loop.ShouldBeFalse();
        component.PlayOnAwake.ShouldBeFalse();
        component.Is3D.ShouldBeTrue();
        component.MinDistance.ShouldBe(1.0f);
        component.MaxDistance.ShouldBe(100.0f);
        component.IsPlaying.ShouldBeFalse();
    }

    [Fact]
    public void AudioSourceComponent_ParameterizedConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var mockClip = Substitute.For<IAudioClip>();

        // Act
        var component = new AudioSourceComponent(
            mockClip,
            volume: 0.8f,
            pitch: 1.2f,
            loop: true,
            playOnAwake: true,
            is3D: false,
            minDistance: 5.0f,
            maxDistance: 50.0f);

        // Assert
        component.AudioClip.ShouldBe(mockClip);
        component.Volume.ShouldBe(0.8f);
        component.Pitch.ShouldBe(1.2f);
        component.Loop.ShouldBeTrue();
        component.PlayOnAwake.ShouldBeTrue();
        component.Is3D.ShouldBeFalse();
        component.MinDistance.ShouldBe(5.0f);
        component.MaxDistance.ShouldBe(50.0f);
    }

    [Fact]
    public void AudioSourceComponent_SetVolume_ShouldAcceptValidRange()
    {
        // Arrange
        var component = new AudioSourceComponent();

        // Act & Assert
        component.Volume = 0.0f;
        component.Volume.ShouldBe(0.0f);

        component.Volume = 0.5f;
        component.Volume.ShouldBe(0.5f);

        component.Volume = 1.0f;
        component.Volume.ShouldBe(1.0f);
    }

    [Fact]
    public void AudioSourceComponent_SetPitch_ShouldUpdateValue()
    {
        // Arrange
        var component = new AudioSourceComponent();

        // Act
        component.Pitch = 2.0f;

        // Assert
        component.Pitch.ShouldBe(2.0f);
    }

    [Fact]
    public void AudioSourceComponent_Set3DProperties_ShouldUpdateValues()
    {
        // Arrange
        var component = new AudioSourceComponent();

        // Act
        component.MinDistance = 10.0f;
        component.MaxDistance = 200.0f;

        // Assert
        component.MinDistance.ShouldBe(10.0f);
        component.MaxDistance.ShouldBe(200.0f);
    }

    [Fact]
    public void AudioSourceComponent_Clone_ShouldCopyAllProperties()
    {
        // Arrange
        var mockClip = Substitute.For<IAudioClip>();
        var original = new AudioSourceComponent(mockClip, 0.7f, 1.5f, true, false, true, 2.0f, 150.0f);

        // Act
        var clone = (AudioSourceComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.AudioClip.ShouldBe(mockClip);
        clone.Volume.ShouldBe(0.7f);
        clone.Pitch.ShouldBe(1.5f);
        clone.Loop.ShouldBeTrue();
        clone.PlayOnAwake.ShouldBeFalse();
        clone.Is3D.ShouldBeTrue();
        clone.MinDistance.ShouldBe(2.0f);
        clone.MaxDistance.ShouldBe(150.0f);
    }

    [Fact]
    public void AudioSourceComponent_AudioClipPath_ShouldReturnNullWhenNoClip()
    {
        // Arrange
        var component = new AudioSourceComponent();

        // Act
        var path = component.AudioClipPath;

        // Assert
        path.ShouldBeNull();
    }

    [Fact]
    public void AudioSourceComponent_AudioClipPath_ShouldReturnPathWhenExplicitlySet()
    {
        // AudioClipPath is now a stored property independent of AudioClip.
        // Setting AudioClip alone does not update AudioClipPath; callers must set it explicitly.
        var mockClip = Substitute.For<IAudioClip>();
        mockClip.Path.Returns("audio/test.wav");
        var component = new AudioSourceComponent
        {
            AudioClip = mockClip,
            AudioClipPath = mockClip.Path
        };

        component.AudioClipPath.ShouldBe("audio/test.wav");
    }
}