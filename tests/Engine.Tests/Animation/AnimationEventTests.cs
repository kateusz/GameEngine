using System.Numerics;
using ECS;
using Engine.Animation;
using Engine.Animation.Events;

namespace Engine.Tests.Animation;

/// <summary>
/// Unit tests for animation event classes.
/// </summary>
public class AnimationEventTests
{
    [Fact]
    public void AnimationFrameEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        var frame = new AnimationFrame 
        { 
            Rect = new Rectangle(0, 0, 32, 32),
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            Events = [],
            TexCoords = new Vector2[4]
        };

        // Act
        var evt = new AnimationFrameEvent(entity, "walk", "footstep", 3, frame);

        // Assert
        Assert.Equal(entity, evt.Entity);
        Assert.Equal("walk", evt.ClipName);
        Assert.Equal("footstep", evt.EventName);
        Assert.Equal(3, evt.FrameIndex);
        Assert.Equal(frame, evt.Frame);
    }

    [Fact]
    public void AnimationFrameEvent_Constructor_WithoutFrame_SetsFrameToNull()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act
        var evt = new AnimationFrameEvent(entity, "idle", "blink", 1);

        // Assert
        Assert.Equal(entity, evt.Entity);
        Assert.Equal("idle", evt.ClipName);
        Assert.Equal("blink", evt.EventName);
        Assert.Equal(1, evt.FrameIndex);
        Assert.Null(evt.Frame);
    }

    [Fact]
    public void AnimationFrameEvent_PropertiesCanBeSet()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity2");
        var frame = new AnimationFrame 
        { 
            Rect = new Rectangle(0, 0, 16, 16),
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            Events = [],
            TexCoords = new Vector2[4]
        };
        var evt = new AnimationFrameEvent(entity1, "run", "dust", 2)
        {
            // Act
            Entity = entity2,
            ClipName = "jump",
            EventName = "land",
            FrameIndex = 5,
            Frame = frame
        };

        // Assert
        Assert.Equal(entity2, evt.Entity);
        Assert.Equal("jump", evt.ClipName);
        Assert.Equal("land", evt.EventName);
        Assert.Equal(5, evt.FrameIndex);
        Assert.Equal(frame, evt.Frame);
    }

    [Fact]
    public void AnimationFrameEvent_DefaultProperties_HaveCorrectValues()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act - Use object initializer
        var evt = new AnimationFrameEvent(entity, "", "", 0)
        {
            // Properties set via constructor, checking defaults
        };

        // Assert
        Assert.Equal(entity, evt.Entity);
        Assert.Equal(string.Empty, evt.ClipName);
        Assert.Equal(string.Empty, evt.EventName);
        Assert.Equal(0, evt.FrameIndex);
        Assert.Null(evt.Frame);
    }

    [Fact]
    public void AnimationFrameEvent_WithComplexEventNames_StoresCorrectly()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        var eventNames = new[]
        {
            "footstep_left",
            "weapon_swing_start",
            "camera_shake_medium",
            "spawn_particle_effect"
        };

        // Act & Assert
        foreach (var eventName in eventNames)
        {
            var evt = new AnimationFrameEvent(entity, "attack", eventName, 0);
            Assert.Equal(eventName, evt.EventName);
        }
    }

    [Fact]
    public void AnimationCompleteEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act
        var evt = new AnimationCompleteEvent(entity, "death");

        // Assert
        Assert.Equal(entity, evt.Entity);
        Assert.Equal("death", evt.ClipName);
    }

    [Fact]
    public void AnimationCompleteEvent_PropertiesCanBeSet()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity2");
        var evt = new AnimationCompleteEvent(entity1, "attack")
        {
            // Act
            Entity = entity2,
            ClipName = "jump"
        };

        // Assert
        Assert.Equal(entity2, evt.Entity);
        Assert.Equal("jump", evt.ClipName);
    }

    [Fact]
    public void AnimationCompleteEvent_DefaultProperties_HaveCorrectValues()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act
        var evt = new AnimationCompleteEvent(entity, "");

        // Assert
        Assert.Equal(entity, evt.Entity);
        Assert.Equal(string.Empty, evt.ClipName);
    }

    [Fact]
    public void AnimationCompleteEvent_WithVariousClipNames_StoresCorrectly()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        var clipNames = new[] { "intro", "attack", "death", "special_move" };

        // Act & Assert
        foreach (var clipName in clipNames)
        {
            var evt = new AnimationCompleteEvent(entity, clipName);
            Assert.Equal(clipName, evt.ClipName);
        }
    }

    [Fact]
    public void AnimationFrameEvent_WithZeroFrameIndex_IsValid()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act
        var evt = new AnimationFrameEvent(entity, "idle", "start", 0);

        // Assert
        Assert.Equal(0, evt.FrameIndex);
    }

    [Fact]
    public void AnimationFrameEvent_WithHighFrameIndex_IsValid()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act
        var evt = new AnimationFrameEvent(entity, "long_animation", "event", 999);

        // Assert
        Assert.Equal(999, evt.FrameIndex);
    }

    [Fact]
    public void AnimationFrameEvent_WithFullFrameData_AccessesFrameProperties()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        var frame = new AnimationFrame
        {
            Rect = new Rectangle(64, 32, 48, 48),
            Pivot = new Vector2(0.5f, 0.0f),
            Scale = Vector2.One,
            TexCoords = new Vector2[4],
            Events = ["sound_fx", "particle_spawn"]
        };

        // Act
        var evt = new AnimationFrameEvent(entity, "spell_cast", "effect", 7, frame);

        // Assert
        Assert.NotNull(evt.Frame);
        Assert.Equal(64, evt.Frame.Rect.X);
        Assert.Equal(32, evt.Frame.Rect.Y);
        Assert.Equal(48, evt.Frame.Rect.Width);
        Assert.Equal(48, evt.Frame.Rect.Height);
        Assert.Equal(0.5f, evt.Frame.Pivot.X);
        Assert.Equal(2, evt.Frame.Events.Length);
    }

    [Fact]
    public void AnimationEvents_DifferentEntityInstances_AreIndependent()
    {
        // Arrange
        var entity1 = Entity.Create(1, "Entity1");
        var entity2 = Entity.Create(2, "Entity2");

        // Act
        var evt1 = new AnimationFrameEvent(entity1, "clip1", "event1", 1);
        var evt2 = new AnimationCompleteEvent(entity2, "clip2");

        // Assert
        Assert.NotEqual(evt1.Entity, evt2.Entity);
    }
}
