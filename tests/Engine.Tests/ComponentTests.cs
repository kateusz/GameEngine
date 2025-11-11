using System.Numerics;
using Bogus;
using Engine.Scene;
using Engine.Scene.Components;
using NSubstitute;
using Shouldly;
using Xunit;
using Engine.Audio;

namespace Engine.Tests;

public class ComponentTests
{
    private readonly Faker _faker = new();

    #region TagComponent Tests

    [Fact]
    public void TagComponent_DefaultConstructor_ShouldInitializeWithEmptyString()
    {
        // Act
        var component = new TagComponent();

        // Assert
        component.Tag.ShouldBe(string.Empty);
    }

    [Fact]
    public void TagComponent_ParameterizedConstructor_ShouldSetTag()
    {
        // Arrange
        var tag = _faker.Lorem.Word();

        // Act
        var component = new TagComponent(tag);

        // Assert
        component.Tag.ShouldBe(tag);
    }

    [Fact]
    public void TagComponent_Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new TagComponent("original-tag");

        // Act
        var clone = (TagComponent)original.Clone();
        clone.Tag = "modified-tag";

        // Assert
        clone.ShouldNotBeSameAs(original);
        original.Tag.ShouldBe("original-tag");
        clone.Tag.ShouldBe("modified-tag");
    }

    #endregion

    #region IDComponent Tests

    [Fact]
    public void IdComponent_DefaultConstructor_ShouldInitializeWithZero()
    {
        // Act
        var component = new IdComponent();

        // Assert
        component.Id.ShouldBe(0);
    }

    [Fact]
    public void IdComponent_ParameterizedConstructor_ShouldSetId()
    {
        // Arrange
        var id = _faker.Random.Long(1, 1000000);

        // Act
        var component = new IdComponent(id);

        // Assert
        component.Id.ShouldBe(id);
    }

    [Fact]
    public void IdComponent_Clone_ShouldCopyId()
    {
        // Arrange
        var original = new IdComponent(12345);

        // Act
        var clone = (IdComponent)original.Clone();

        // Assert
        clone.Id.ShouldBe(original.Id);
        clone.ShouldNotBeSameAs(original);
    }

    [Fact]
    public void IdComponent_SetId_ShouldUpdateValue()
    {
        // Arrange
        var component = new IdComponent();

        // Act
        component.Id = 99999;

        // Assert
        component.Id.ShouldBe(99999);
    }

    #endregion

    #region SpriteRendererComponent Tests

    [Fact]
    public void SpriteRendererComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var component = new SpriteRendererComponent();

        // Assert
        component.Color.ShouldBe(Vector4.One);
        component.Texture.ShouldBeNull();
        component.TilingFactor.ShouldBe(1.0f);
    }

    [Fact]
    public void SpriteRendererComponent_ColorConstructor_ShouldSetColor()
    {
        // Arrange
        var color = new Vector4(1f, 0f, 0f, 1f); // Red

        // Act
        var component = new SpriteRendererComponent(color);

        // Assert
        component.Color.ShouldBe(color);
        component.Texture.ShouldBeNull();
        component.TilingFactor.ShouldBe(1.0f);
    }

    [Fact]
    public void SpriteRendererComponent_FullConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var color = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        var tilingFactor = 2.5f;

        // Act
        var component = new SpriteRendererComponent(color, null, tilingFactor);

        // Assert
        component.Color.ShouldBe(color);
        component.Texture.ShouldBeNull();
        component.TilingFactor.ShouldBe(tilingFactor);
    }

    [Fact]
    public void SpriteRendererComponent_SetColor_ShouldUpdateValue()
    {
        // Arrange
        var component = new SpriteRendererComponent();
        var newColor = new Vector4(0.2f, 0.4f, 0.6f, 0.8f);

        // Act
        component.Color = newColor;

        // Assert
        component.Color.ShouldBe(newColor);
    }

    [Fact]
    public void SpriteRendererComponent_SetTilingFactor_ShouldUpdateValue()
    {
        // Arrange
        var component = new SpriteRendererComponent();

        // Act
        component.TilingFactor = 3.0f;

        // Assert
        component.TilingFactor.ShouldBe(3.0f);
    }

    [Fact]
    public void SpriteRendererComponent_Clone_ShouldCopyAllProperties()
    {
        // Arrange
        var color = new Vector4(0.1f, 0.2f, 0.3f, 0.4f);
        var original = new SpriteRendererComponent(color, null, 2.0f);

        // Act
        var clone = (SpriteRendererComponent)original.Clone();
        clone.Color = Vector4.Zero;

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.TilingFactor.ShouldBe(2.0f);
        original.Color.ShouldBe(color);
        clone.Color.ShouldBe(Vector4.Zero);
    }

    #endregion

    #region BoxCollider2DComponent Tests

    [Fact]
    public void BoxCollider2DComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var component = new BoxCollider2DComponent();

        // Assert
        component.Size.ShouldBe(Vector2.Zero);
        component.Offset.ShouldBe(Vector2.Zero);
        component.Density.ShouldBe(1.0f);
        component.Friction.ShouldBe(0.5f);
        component.Restitution.ShouldBe(0.0f);
        component.RestitutionThreshold.ShouldBe(0.5f);
        component.IsTrigger.ShouldBeFalse();
        component.IsDirty.ShouldBeTrue(); // Initially dirty
    }

    [Fact]
    public void BoxCollider2DComponent_ParameterizedConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var size = new Vector2(2f, 3f);
        var offset = new Vector2(0.5f, 0.5f);

        // Act
        var component = new BoxCollider2DComponent(size, offset, 2.0f, 0.3f, 0.8f, 1.0f, true);

        // Assert
        component.Size.ShouldBe(size);
        component.Offset.ShouldBe(offset);
        component.Density.ShouldBe(2.0f);
        component.Friction.ShouldBe(0.3f);
        component.Restitution.ShouldBe(0.8f);
        component.RestitutionThreshold.ShouldBe(1.0f);
        component.IsTrigger.ShouldBeTrue();
        component.IsDirty.ShouldBeTrue(); // Initially dirty
    }

    [Fact]
    public void BoxCollider2DComponent_SetDensity_ShouldMarkAsDirty()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.ClearDirtyFlag();
        component.IsDirty.ShouldBeFalse();

        // Act
        component.Density = 5.0f;

        // Assert
        component.Density.ShouldBe(5.0f);
        component.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void BoxCollider2DComponent_SetDensity_ToSameValue_ShouldNotMarkDirty()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.Density = 3.0f;
        component.ClearDirtyFlag();

        // Act
        component.Density = 3.0f; // Same value

        // Assert
        component.IsDirty.ShouldBeFalse();
    }

    [Fact]
    public void BoxCollider2DComponent_SetFriction_ShouldMarkAsDirty()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.ClearDirtyFlag();

        // Act
        component.Friction = 0.8f;

        // Assert
        component.Friction.ShouldBe(0.8f);
        component.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void BoxCollider2DComponent_SetRestitution_ShouldMarkAsDirty()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.ClearDirtyFlag();

        // Act
        component.Restitution = 0.9f;

        // Assert
        component.Restitution.ShouldBe(0.9f);
        component.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void BoxCollider2DComponent_ClearDirtyFlag_ShouldResetFlag()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.Density = 2.0f;
        component.IsDirty.ShouldBeTrue();

        // Act
        component.ClearDirtyFlag();

        // Assert
        component.IsDirty.ShouldBeFalse();
    }

    [Fact]
    public void BoxCollider2DComponent_MultiplePropertyChanges_ShouldStayDirty()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.ClearDirtyFlag();

        // Act
        component.Density = 2.0f;
        component.Friction = 0.7f;
        component.Restitution = 0.5f;

        // Assert
        component.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void BoxCollider2DComponent_Clone_ShouldCopyAllProperties()
    {
        // Arrange
        var original = new BoxCollider2DComponent(
            new Vector2(10f, 10f),
            new Vector2(1f, 1f),
            3.0f, 0.6f, 0.4f, 0.8f, true);

        // Act
        var clone = (BoxCollider2DComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Size.ShouldBe(original.Size);
        clone.Offset.ShouldBe(original.Offset);
        clone.Density.ShouldBe(original.Density);
        clone.Friction.ShouldBe(original.Friction);
        clone.Restitution.ShouldBe(original.Restitution);
        clone.RestitutionThreshold.ShouldBe(original.RestitutionThreshold);
        clone.IsTrigger.ShouldBe(original.IsTrigger);
    }

    #endregion

    #region RigidBody2DComponent Tests

    [Fact]
    public void RigidBody2DComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var component = new RigidBody2DComponent();

        // Assert
        component.BodyType.ShouldBe(RigidBodyType.Static);
        component.FixedRotation.ShouldBeFalse();
        component.RuntimeBody.ShouldBeNull();
    }

    [Fact]
    public void RigidBody2DComponent_SetBodyType_ShouldUpdateValue()
    {
        // Arrange
        var component = new RigidBody2DComponent();

        // Act
        component.BodyType = RigidBodyType.Dynamic;

        // Assert
        component.BodyType.ShouldBe(RigidBodyType.Dynamic);
    }

    [Fact]
    public void RigidBody2DComponent_SetFixedRotation_ShouldUpdateValue()
    {
        // Arrange
        var component = new RigidBody2DComponent();

        // Act
        component.FixedRotation = true;

        // Assert
        component.FixedRotation.ShouldBeTrue();
    }

    [Fact]
    public void RigidBody2DComponent_Clone_ShouldCopyProperties_WithoutRuntimeBody()
    {
        // Arrange
        var original = new RigidBody2DComponent
        {
            BodyType = RigidBodyType.Kinematic,
            FixedRotation = true,
            RuntimeBody = null // Would be a Box2D body at runtime
        };

        // Act
        var clone = (RigidBody2DComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.BodyType.ShouldBe(RigidBodyType.Kinematic);
        clone.FixedRotation.ShouldBeTrue();
        clone.RuntimeBody.ShouldBeNull(); // Should not clone runtime body
    }

    [Theory]
    [InlineData(RigidBodyType.Static)]
    [InlineData(RigidBodyType.Dynamic)]
    [InlineData(RigidBodyType.Kinematic)]
    public void RigidBody2DComponent_AllBodyTypes_ShouldBeSettable(RigidBodyType bodyType)
    {
        // Arrange
        var component = new RigidBody2DComponent();

        // Act
        component.BodyType = bodyType;

        // Assert
        component.BodyType.ShouldBe(bodyType);
    }

    #endregion

    #region AudioSourceComponent Tests

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
    public void AudioSourceComponent_AudioClipPath_ShouldReturnPathWhenClipExists()
    {
        // Arrange
        var mockClip = Substitute.For<IAudioClip>();
        mockClip.Path.Returns("audio/test.wav");
        var component = new AudioSourceComponent { AudioClip = mockClip };

        // Act
        var path = component.AudioClipPath;

        // Assert
        path.ShouldBe("audio/test.wav");
    }

    #endregion

    #region CameraComponent Tests

    [Fact]
    public void CameraComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var component = new CameraComponent();

        // Assert
        component.Camera.ShouldNotBeNull();
        component.Primary.ShouldBeTrue();
        component.FixedAspectRatio.ShouldBeFalse();
    }

    [Fact]
    public void CameraComponent_SetPrimary_ShouldUpdateValue()
    {
        // Arrange
        var component = new CameraComponent();

        // Act
        component.Primary = false;

        // Assert
        component.Primary.ShouldBeFalse();
    }

    [Fact]
    public void CameraComponent_SetFixedAspectRatio_ShouldUpdateValue()
    {
        // Arrange
        var component = new CameraComponent();

        // Act
        component.FixedAspectRatio = true;

        // Assert
        component.FixedAspectRatio.ShouldBeTrue();
    }

    [Fact]
    public void CameraComponent_Clone_ShouldCopyAllCameraProperties()
    {
        // Arrange
        var original = new CameraComponent
        {
            Primary = false,
            FixedAspectRatio = true
        };
        original.Camera.SetOrthographic(15f, -2f, 2f);
        original.Camera.SetViewportSize(1920, 1080);

        // Act
        var clone = (CameraComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Camera.ShouldNotBeSameAs(original.Camera);
        clone.Primary.ShouldBe(false);
        clone.FixedAspectRatio.ShouldBeTrue();
        clone.Camera.ProjectionType.ShouldBe(original.Camera.ProjectionType);
        clone.Camera.OrthographicSize.ShouldBe(original.Camera.OrthographicSize);
        clone.Camera.OrthographicNear.ShouldBe(original.Camera.OrthographicNear);
        clone.Camera.OrthographicFar.ShouldBe(original.Camera.OrthographicFar);
        clone.Camera.AspectRatio.ShouldBe(original.Camera.AspectRatio);
    }

    [Fact]
    public void CameraComponent_Clone_ShouldCopyPerspectiveProperties()
    {
        // Arrange
        var original = new CameraComponent();
        original.Camera.SetPerspective(MathF.PI / 3, 0.5f, 2000f);

        // Act
        var clone = (CameraComponent)original.Clone();

        // Assert
        clone.Camera.ProjectionType.ShouldBe(ProjectionType.Perspective);
        clone.Camera.PerspectiveFOV.ShouldBe(original.Camera.PerspectiveFOV);
        clone.Camera.PerspectiveNear.ShouldBe(original.Camera.PerspectiveNear);
        clone.Camera.PerspectiveFar.ShouldBe(original.Camera.PerspectiveFar);
    }

    [Fact]
    public void CameraComponent_Clone_ModifyingClone_ShouldNotAffectOriginal()
    {
        // Arrange
        var original = new CameraComponent();
        original.Camera.SetOrthographic(10f, -1f, 1f);

        // Act
        var clone = (CameraComponent)original.Clone();
        clone.Camera.SetOrthographic(20f, -2f, 2f);

        // Assert
        original.Camera.OrthographicSize.ShouldBe(10f);
        clone.Camera.OrthographicSize.ShouldBe(20f);
    }

    #endregion

    #region Component Integration Tests

    [Fact]
    public void AllComponents_Clone_ShouldImplementIComponent()
    {
        // Arrange & Act & Assert
        new TagComponent().ShouldBeAssignableTo<ECS.IComponent>();
        new IdComponent().ShouldBeAssignableTo<ECS.IComponent>();
        new SpriteRendererComponent().ShouldBeAssignableTo<ECS.IComponent>();
        new BoxCollider2DComponent().ShouldBeAssignableTo<ECS.IComponent>();
        new RigidBody2DComponent().ShouldBeAssignableTo<ECS.IComponent>();
        new AudioSourceComponent().ShouldBeAssignableTo<ECS.IComponent>();
        new CameraComponent().ShouldBeAssignableTo<ECS.IComponent>();
        new TransformComponent().ShouldBeAssignableTo<ECS.IComponent>();
    }

    [Fact]
    public void AllComponents_Clone_ShouldReturnNonNullComponent()
    {
        // Arrange
        var components = new ECS.IComponent[]
        {
            new TagComponent(),
            new IdComponent(),
            new SpriteRendererComponent(),
            new BoxCollider2DComponent(),
            new RigidBody2DComponent(),
            new AudioSourceComponent(),
            new CameraComponent(),
            new TransformComponent()
        };

        // Act & Assert
        foreach (var component in components)
        {
            var clone = component.Clone();
            clone.ShouldNotBeNull();
            clone.ShouldBeAssignableTo<ECS.IComponent>();
        }
    }

    #endregion
}
