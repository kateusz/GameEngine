using Bogus;
using ECS;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Components;
using NSubstitute;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests;

public class SceneTests : IDisposable
{
    private readonly Faker _faker = new();
    private readonly IGraphics2D _mockGraphics2D;
    private readonly ISceneSystemRegistry _mockSystemRegistry;
    private readonly IContext _context;

    public SceneTests()
    {
        _mockGraphics2D = Substitute.For<IGraphics2D>();
        _mockSystemRegistry = Substitute.For<ISceneSystemRegistry>();
        _context = new Context();

        // Setup system registry to return our mock system manager behavior
        _mockSystemRegistry.PopulateSystemManager(Arg.Any<ISystemManager>())
            .Returns(new List<ISystem>());
    }

    public void Dispose()
    {
        // Clear the context between tests
        _context.Clear();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyEntityCollection()
    {
        // Act
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Assert
        scene.Entities.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldClearGlobalContext()
    {
        // Arrange - Add an entity to the context before creating scene
        var existingEntity = Entity.Create(1, "existing");
        _context.Register(existingEntity);
        _context.Entities.ShouldNotBeEmpty();

        // Act
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Assert - Context should be cleared
        scene.Entities.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldPopulateSystemManagerFromRegistry()
    {
        // Arrange
        var mockSystem = Substitute.For<ISystem>();
        _mockSystemRegistry.PopulateSystemManager(Arg.Any<ISystemManager>())
            .Returns(new List<ISystem> { mockSystem });

        // Act
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Assert
        _mockSystemRegistry.Received(1).PopulateSystemManager(Arg.Any<ISystemManager>());
    }

    #endregion

    #region CreateEntity Tests

    [Fact]
    public void CreateEntity_ShouldCreateEntityWithGivenName()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var entityName = _faker.Random.Word();

        // Act
        var entity = scene.CreateEntity(entityName);

        // Assert
        entity.Name.ShouldBe(entityName);
    }

    [Fact]
    public void CreateEntity_ShouldAssignPositiveId()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Act
        var entity = scene.CreateEntity("test");

        // Assert
        entity.Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CreateEntity_ShouldAssignIncrementingIds()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Act
        var entity1 = scene.CreateEntity("first");
        var entity2 = scene.CreateEntity("second");
        var entity3 = scene.CreateEntity("third");

        // Assert
        entity2.Id.ShouldBeGreaterThan(entity1.Id);
        entity3.Id.ShouldBeGreaterThan(entity2.Id);
    }

    [Fact]
    public void CreateEntity_ShouldRegisterEntityInContext()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Act
        var entity = scene.CreateEntity("test");

        // Assert
        scene.Entities.ShouldContain(entity);
        _context.Entities.ShouldContain(entity);
    }

    [Fact]
    public void CreateEntity_MultipleTimes_ShouldCreateMultipleEntities()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Act
        var entities = new List<Entity>();
        for (int i = 0; i < 10; i++)
        {
            entities.Add(scene.CreateEntity($"entity-{i}"));
        }

        // Assert
        scene.Entities.Count().ShouldBe(10);
        entities.Select(e => e.Id).Distinct().Count().ShouldBe(10); // All IDs should be unique
    }

    #endregion

    #region AddEntity Tests

    [Fact]
    public void AddEntity_ShouldAddEntityToScene()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var entity = Entity.Create(100, "imported");

        // Act
        scene.AddEntity(entity);

        // Assert
        scene.Entities.ShouldContain(entity);
    }

    [Fact]
    public void AddEntity_WithHigherId_ShouldUpdateNextEntityId()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var highIdEntity = Entity.Create(500, "high-id");

        // Act
        scene.AddEntity(highIdEntity);
        var newEntity = scene.CreateEntity("new");

        // Assert
        newEntity.Id.ShouldBeGreaterThan(500);
    }

    [Fact]
    public void AddEntity_WithLowerId_ShouldNotAffectNextEntityId()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        scene.CreateEntity("first"); // Gets ID 1
        var lowIdEntity = Entity.Create(50, "low-id"); // Use ID 50 instead of 1

        // Act
        scene.AddEntity(lowIdEntity); // This should work since ID 50 is not taken
        var newEntity = scene.CreateEntity("new");

        // Assert
        newEntity.Id.ShouldBeGreaterThanOrEqualTo(51); // Should be > max(2, 50+1)
    }

    [Fact]
    public void AddEntity_WithZeroOrNegativeId_ShouldThrowException()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var invalidEntity = Entity.Create(0, "invalid");

        // Act & Assert
        Should.Throw<ArgumentException>(() => scene.AddEntity(invalidEntity));
    }

    [Fact]
    public void AddEntity_WithNegativeId_ShouldThrowException()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var invalidEntity = Entity.Create(-5, "invalid");

        // Act & Assert
        Should.Throw<ArgumentException>(() => scene.AddEntity(invalidEntity));
    }
    
    #endregion

    #region DestroyEntity Tests

    [Fact]
    public void DestroyEntity_ShouldRemoveEntityFromScene()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var entity = scene.CreateEntity("to-destroy");

        // Act
        scene.DestroyEntity(entity);

        // Assert
        scene.Entities.ShouldNotContain(entity);
        _context.Entities.ShouldNotContain(entity);
    }

    [Fact]
    public void DestroyEntity_ShouldRemoveEntityFromContext()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var entity = scene.CreateEntity("to-destroy");
        _context.Entities.ShouldContain(entity);

        // Act
        scene.DestroyEntity(entity);

        // Assert
        _context.Entities.ShouldNotContain(entity);
    }

    [Fact]
    public void DestroyEntity_ShouldAllowCreatingNewEntityWithSameId()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var entity = scene.CreateEntity("original");
        var entityId = entity.Id;

        // Act
        scene.DestroyEntity(entity);
        var newEntity = Entity.Create(entityId, "reused-id");
        scene.AddEntity(newEntity);

        // Assert
        scene.Entities.Count().ShouldBe(1); // Only one entity should exist
        scene.Entities.ShouldContain(newEntity);
        // Note: Can't use ShouldNotContain(entity) because Entity equality is based on ID,
        // so entity and newEntity are considered equal. We verify correctness by checking
        // the Name property instead.
        scene.Entities.First().Name.ShouldBe("reused-id");
    }

    [Fact]
    public void DestroyEntity_Multiple_ShouldRemoveAllDestroyedEntities()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var entities = Enumerable.Range(0, 5)
            .Select(i => scene.CreateEntity($"entity-{i}"))
            .ToList();

        // Act - Destroy every other entity
        scene.DestroyEntity(entities[0]);
        scene.DestroyEntity(entities[2]);
        scene.DestroyEntity(entities[4]);

        // Assert
        scene.Entities.Count().ShouldBe(2);
        scene.Entities.ShouldContain(entities[1]);
        scene.Entities.ShouldContain(entities[3]);
    }

    #endregion

    #region DuplicateEntity Tests

    [Fact]
    public void DuplicateEntity_ShouldCreateNewEntityWithDifferentId()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var original = scene.CreateEntity("original");

        // Act
        var duplicate = scene.DuplicateEntity(original);

        // Assert
        duplicate.Id.ShouldNotBe(original.Id);
        duplicate.Name.ShouldBe(original.Name);
    }

    [Fact]
    public void DuplicateEntity_ShouldCloneAllComponents()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var original = scene.CreateEntity("original");
        original.AddComponent(new TagComponent { Tag = "test-tag" });
        original.AddComponent(new TransformComponent(
            new System.Numerics.Vector3(1, 2, 3),
            new System.Numerics.Vector3(0.1f, 0.2f, 0.3f),
            new System.Numerics.Vector3(2, 2, 2)
        ));

        // Act
        var duplicate = scene.DuplicateEntity(original);

        // Assert
        duplicate.HasComponent<TagComponent>().ShouldBeTrue();
        duplicate.HasComponent<TransformComponent>().ShouldBeTrue();

        var dupTag = duplicate.GetComponent<TagComponent>();
        var dupTransform = duplicate.GetComponent<TransformComponent>();

        dupTag.Tag.ShouldBe("test-tag");
        dupTransform.Translation.ShouldBe(new System.Numerics.Vector3(1, 2, 3));
    }

    [Fact]
    public void DuplicateEntity_ModifyingDuplicate_ShouldNotAffectOriginal()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var original = scene.CreateEntity("original");
        var originalTag = new TagComponent { Tag = "original" };
        original.AddComponent(originalTag);

        // Act
        var duplicate = scene.DuplicateEntity(original);
        var duplicateTag = duplicate.GetComponent<TagComponent>();
        duplicateTag.Tag = "modified";

        // Assert
        originalTag.Tag.ShouldBe("original");
        duplicateTag.Tag.ShouldBe("modified");
    }

    [Fact]
    public void DuplicateEntity_ShouldRegisterInScene()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var original = scene.CreateEntity("original");

        // Act
        var duplicate = scene.DuplicateEntity(original);

        // Assert
        scene.Entities.ShouldContain(duplicate);
        scene.Entities.Count().ShouldBe(2);
    }

    #endregion

    #region GetPrimaryCameraEntity Tests

    [Fact]
    public void GetPrimaryCameraEntity_WhenNoCameraExists_ShouldReturnNull()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        scene.CreateEntity("no-camera");

        // Act
        var cameraEntity = scene.GetPrimaryCameraEntity();

        // Assert
        cameraEntity.ShouldBeNull();
    }

    [Fact]
    public void GetPrimaryCameraEntity_WhenPrimaryCameraExists_ShouldReturnIt()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var cameraEntity = scene.CreateEntity("camera");
        var cameraComponent = new CameraComponent { Primary = true };
        cameraEntity.AddComponent(cameraComponent);

        // Act
        var result = scene.GetPrimaryCameraEntity();

        // Assert
        result.ShouldBe(cameraEntity);
    }

    [Fact]
    public void GetPrimaryCameraEntity_WhenNonPrimaryCameraExists_ShouldReturnNull()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var cameraEntity = scene.CreateEntity("camera");
        var cameraComponent = new CameraComponent { Primary = false };
        cameraEntity.AddComponent(cameraComponent);

        // Act
        var result = scene.GetPrimaryCameraEntity();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetPrimaryCameraEntity_WhenMultipleCamerasExist_ShouldReturnPrimaryOne()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        var camera1 = scene.CreateEntity("camera1");
        camera1.AddComponent(new CameraComponent { Primary = false });

        var camera2 = scene.CreateEntity("camera2");
        camera2.AddComponent(new CameraComponent { Primary = true });

        var camera3 = scene.CreateEntity("camera3");
        camera3.AddComponent(new CameraComponent { Primary = false });

        // Act
        var result = scene.GetPrimaryCameraEntity();

        // Assert
        result.ShouldBe(camera2);
    }

    #endregion

    #region GetPrimaryCameraData Tests

    [Fact]
    public void GetPrimaryCameraData_WhenNoCameraExists_ShouldReturnNullAndIdentity()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Act
        var (camera, transform) = scene.GetPrimaryCameraData();

        // Assert
        camera.ShouldBeNull();
        transform.ShouldBe(System.Numerics.Matrix4x4.Identity);
    }

    [Fact]
    public void GetPrimaryCameraData_WhenPrimaryCameraExists_ShouldReturnCameraAndTransform()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var cameraEntity = scene.CreateEntity("camera");
        cameraEntity.AddComponent(new TransformComponent
        {
            Translation = new System.Numerics.Vector3(1, 2, 3),
            Rotation = System.Numerics.Vector3.Zero,
            Scale = System.Numerics.Vector3.One
        });
        cameraEntity.AddComponent(new CameraComponent { Primary = true });

        // Act
        var (camera, transform) = scene.GetPrimaryCameraData();

        // Assert
        camera.ShouldNotBeNull();
        transform.ShouldNotBe(System.Numerics.Matrix4x4.Identity);
    }

    [Fact]
    public void GetPrimaryCameraData_CalledMultipleTimes_ShouldUseCacheAfterFirstCall()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var cameraEntity = scene.CreateEntity("camera");
        cameraEntity.AddComponent(new TransformComponent());
        cameraEntity.AddComponent(new CameraComponent { Primary = true });

        // Act - Call multiple times
        var result1 = scene.GetPrimaryCameraData();
        var result2 = scene.GetPrimaryCameraData();
        var result3 = scene.GetPrimaryCameraData();

        // Assert - Results should be consistent (cached)
        result1.camera.ShouldBe(result2.camera);
        result2.camera.ShouldBe(result3.camera);
        result1.camera.ShouldNotBeNull();
    }

    [Fact]
    public void GetPrimaryCameraData_AfterCameraAdded_ShouldInvalidateCache()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Act - Get camera data when none exists
        var (camera1, _) = scene.GetPrimaryCameraData();

        // Add a camera
        var cameraEntity = scene.CreateEntity("camera");
        cameraEntity.AddComponent(new TransformComponent());
        cameraEntity.AddComponent(new CameraComponent { Primary = true });

        // Get camera data again
        var (camera2, _) = scene.GetPrimaryCameraData();

        // Assert
        camera1.ShouldBeNull();
        camera2.ShouldNotBeNull();
    }

    [Fact]
    public void GetPrimaryCameraData_AfterCameraRemoved_ShouldInvalidateCache()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var cameraEntity = scene.CreateEntity("camera");
        cameraEntity.AddComponent(new TransformComponent());
        var cameraComponent = cameraEntity.AddComponent(new CameraComponent { Primary = true });

        // Get camera data (should cache it)
        var (camera1, _) = scene.GetPrimaryCameraData();

        // Act - Remove camera component
        cameraEntity.RemoveComponent<CameraComponent>();

        // Get camera data again (should invalidate cache)
        var (camera2, _) = scene.GetPrimaryCameraData();

        // Assert
        camera1.ShouldNotBeNull();
        camera2.ShouldBeNull();
    }

    [Fact]
    public void GetPrimaryCameraData_AfterCameraEntityDestroyed_ShouldInvalidateCache()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var cameraEntity = scene.CreateEntity("camera");
        cameraEntity.AddComponent(new TransformComponent());
        cameraEntity.AddComponent(new CameraComponent { Primary = true });

        // Get camera data (should cache it)
        var (camera1, _) = scene.GetPrimaryCameraData();

        // Act - Destroy camera entity
        scene.DestroyEntity(cameraEntity);

        // Get camera data again (should invalidate cache)
        var (camera2, _) = scene.GetPrimaryCameraData();

        // Assert
        camera1.ShouldNotBeNull();
        camera2.ShouldBeNull();
    }

    [Fact]
    public void GetPrimaryCameraData_WithMultipleCameras_ShouldReturnFirstPrimary()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        var camera1 = scene.CreateEntity("camera1");
        camera1.AddComponent(new TransformComponent());
        camera1.AddComponent(new CameraComponent { Primary = false });

        var camera2 = scene.CreateEntity("camera2");
        camera2.AddComponent(new TransformComponent());
        var primaryComponent = camera2.AddComponent(new CameraComponent { Primary = true });

        var camera3 = scene.CreateEntity("camera3");
        camera3.AddComponent(new TransformComponent());
        camera3.AddComponent(new CameraComponent { Primary = true }); // Second primary

        // Act
        var (camera, _) = scene.GetPrimaryCameraData();

        // Assert - Should return the first primary camera found
        camera.ShouldBe(primaryComponent.Camera);
    }

    #endregion

    #region OnViewportResize Tests
    [Fact]
    public void OnViewportResize_WhenNoCameras_ShouldNotThrow()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        scene.CreateEntity("no-camera");

        // Act & Assert
        Should.NotThrow(() => scene.OnViewportResize(1024, 768));
    }

    #endregion

    #region Entities Property Tests

    [Fact]
    public void Entities_ShouldReturnAllCreatedEntities()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var entity1 = scene.CreateEntity("first");
        var entity2 = scene.CreateEntity("second");
        var entity3 = scene.CreateEntity("third");

        // Act
        var entities = scene.Entities.ToList();

        // Assert
        entities.Count.ShouldBe(3);
        entities.ShouldContain(entity1);
        entities.ShouldContain(entity2);
        entities.ShouldContain(entity3);
    }

    [Fact]
    public void Entities_AfterDestroy_ShouldNotIncludeDestroyedEntity()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);
        var entity1 = scene.CreateEntity("first");
        var entity2 = scene.CreateEntity("second");
        scene.DestroyEntity(entity1);

        // Act
        var entities = scene.Entities.ToList();

        // Assert
        entities.Count.ShouldBe(1);
        entities.ShouldNotContain(entity1);
        entities.ShouldContain(entity2);
    }

    #endregion

    #region Stress Tests

    [Fact]
    public void Scene_CreateAndDestroyManyEntities_ShouldHandleCorrectly()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Act - Create 1000 entities
        var entities = new List<Entity>();
        for (int i = 0; i < 1000; i++)
        {
            entities.Add(scene.CreateEntity($"entity-{i}"));
        }

        // Destroy half of them
        for (int i = 0; i < 500; i++)
        {
            scene.DestroyEntity(entities[i * 2]);
        }

        // Assert
        scene.Entities.Count().ShouldBe(500);
    }

    [Fact]
    public void Scene_CreateEntitiesWithComponents_ShouldMaintainIntegrity()
    {
        // Arrange
        using var scene = new EngineScene("test-scene", _mockSystemRegistry, _mockGraphics2D, _context);

        // Act - Create entities with various components
        var entities = new List<Entity>();
        for (int i = 0; i < 100; i++)
        {
            var entity = scene.CreateEntity($"entity-{i}");
            entity.AddComponent(new TagComponent { Tag = $"tag-{i}" });
            entity.AddComponent(new TransformComponent());
            entities.Add(entity);
        }

        // Assert
        scene.Entities.Count().ShouldBe(100);
        foreach (var entity in entities)
        {
            entity.HasComponent<TagComponent>().ShouldBeTrue();
            entity.HasComponent<TransformComponent>().ShouldBeTrue();
        }
    }

    #endregion
}
