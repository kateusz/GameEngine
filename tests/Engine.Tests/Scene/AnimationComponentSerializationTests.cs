using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Scene.Components;
using Xunit;

namespace Engine.Tests.Scene;

public class AnimationComponentSerializationTests : IDisposable
{
    private readonly string _testFilePath;

    public AnimationComponentSerializationTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_animation_component_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        // Clean up test file
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public void Serialize_AnimationComponent_ProducesValidJson()
    {
        // Arrange
        var component = new AnimationComponent
        {
            AssetPath = "Animations/player.json",
            CurrentClipName = "Idle",
            IsPlaying = true,
            Loop = true,
            PlaybackSpeed = 1.5f,
            ShowDebugInfo = true,
            CurrentFrameIndex = 5, // Runtime state - should not be serialized
            FrameTimer = 0.25f,    // Runtime state - should not be serialized
            PreviousFrameIndex = 4 // Runtime state - should not be serialized
        };

        // Act - Serialize to JSON
        var json = JsonSerializer.Serialize(component, new JsonSerializerOptions { WriteIndented = true });

        // Assert - Check that JSON contains expected properties
        Assert.Contains("\"AssetPath\": \"Animations/player.json\"", json);
        Assert.Contains("\"CurrentClipName\": \"Idle\"", json);
        Assert.Contains("\"IsPlaying\": true", json);
        Assert.Contains("\"Loop\": true", json);
        Assert.Contains("\"PlaybackSpeed\": 1.5", json);
        Assert.Contains("\"ShowDebugInfo\": true", json);

        // Runtime state should NOT be in JSON (marked with [NonSerialized])
        Assert.DoesNotContain("CurrentFrameIndex", json);
        Assert.DoesNotContain("FrameTimer", json);
        Assert.DoesNotContain("PreviousFrameIndex", json);
    }

    [Fact]
    public void Deserialize_AnimationComponent_RestoresAllPersistedProperties()
    {
        // Arrange - JSON representation of AnimationComponent
        var json = @"{
            ""AssetPath"": ""Animations/player.json"",
            ""CurrentClipName"": ""Idle"",
            ""IsPlaying"": true,
            ""Loop"": true,
            ""PlaybackSpeed"": 1.5,
            ""ShowDebugInfo"": true
        }";

        // Act - Deserialize from JSON
        var component = JsonSerializer.Deserialize<AnimationComponent>(json);

        // Assert
        Assert.NotNull(component);
        Assert.Equal("Animations/player.json", component.AssetPath);
        Assert.Equal("Idle", component.CurrentClipName);
        Assert.True(component.IsPlaying);
        Assert.True(component.Loop);
        Assert.Equal(1.5f, component.PlaybackSpeed);
        Assert.True(component.ShowDebugInfo);

        // Runtime state should be at default values
        Assert.Equal(0, component.CurrentFrameIndex);
        Assert.Equal(0.0f, component.FrameTimer);
        Assert.Equal(-1, component.PreviousFrameIndex);

        // Asset should be null until loaded by AnimationSystem
        Assert.Null(component.Asset);
    }

    [Fact]
    public void Deserialize_AnimationComponent_WithNullAssetPath_Works()
    {
        // Arrange - JSON with null asset path
        var json = @"{
            ""AssetPath"": null,
            ""CurrentClipName"": """",
            ""IsPlaying"": false,
            ""Loop"": false,
            ""PlaybackSpeed"": 1.0,
            ""ShowDebugInfo"": false
        }";

        // Act
        var component = JsonSerializer.Deserialize<AnimationComponent>(json);

        // Assert
        Assert.NotNull(component);
        Assert.Null(component.AssetPath);
        Assert.Equal("", component.CurrentClipName);
        Assert.False(component.IsPlaying);
        Assert.False(component.Loop);
        Assert.Equal(1.0f, component.PlaybackSpeed);
        Assert.False(component.ShowDebugInfo);
    }

    [Fact]
    public void RoundTrip_AnimationComponent_PreservesData()
    {
        // Arrange
        var original = new AnimationComponent
        {
            AssetPath = "Animations/character.json",
            CurrentClipName = "Run",
            IsPlaying = true,
            Loop = false,
            PlaybackSpeed = 2.0f,
            ShowDebugInfo = true
        };

        // Act - Serialize then deserialize
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AnimationComponent>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.AssetPath, deserialized.AssetPath);
        Assert.Equal(original.CurrentClipName, deserialized.CurrentClipName);
        Assert.Equal(original.IsPlaying, deserialized.IsPlaying);
        Assert.Equal(original.Loop, deserialized.Loop);
        Assert.Equal(original.PlaybackSpeed, deserialized.PlaybackSpeed);
        Assert.Equal(original.ShowDebugInfo, deserialized.ShowDebugInfo);
    }
}
