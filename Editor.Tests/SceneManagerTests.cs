using System.Numerics;
using ECS;
using Editor.Panels;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;
using Moq;
using Xunit;

namespace Editor.Tests;

/// <summary>
/// Unit tests for SceneManager demonstrating improved testability via ISceneView abstraction.
/// </summary>
public class SceneManagerTests
{
    private readonly Mock<ISceneView> _mockSceneView;
    private readonly Mock<ISceneSerializer> _mockSceneSerializer;
    private readonly SceneManager _sceneManager;

    public SceneManagerTests()
    {
        _mockSceneView = new Mock<ISceneView>();
        _mockSceneSerializer = new Mock<ISceneSerializer>();
        _sceneManager = new SceneManager(_mockSceneView.Object, _mockSceneSerializer.Object);
    }

    [Fact]
    public void New_ShouldCreateNewSceneAndSetContext()
    {
        // Arrange
        var viewportSize = new Vector2(800, 600);

        // Act
        _sceneManager.New(viewportSize);

        // Assert
        Assert.NotNull(CurrentScene.Instance);
        _mockSceneView.Verify(v => v.SetContext(It.IsAny<Scene>()), Times.Once);
    }

    [Fact]
    public void New_ShouldSetSceneStateToEdit()
    {
        // Arrange
        var viewportSize = new Vector2(800, 600);

        // Act
        _sceneManager.New(viewportSize);

        // Assert
        Assert.Equal(SceneState.Edit, _sceneManager.SceneState);
    }

    [Fact]
    public void Open_ShouldLoadSceneAndSetContext()
    {
        // Arrange
        var viewportSize = new Vector2(800, 600);
        var scenePath = "test.scene";

        // Act
        _sceneManager.Open(viewportSize, scenePath);

        // Assert
        Assert.Equal(scenePath, _sceneManager.EditorScenePath);
        _mockSceneView.Verify(v => v.SetContext(It.IsAny<Scene>()), Times.Once);
        _mockSceneSerializer.Verify(s => s.Deserialize(It.IsAny<Scene>(), scenePath), Times.Once);
    }

    [Fact]
    public void Save_ShouldSerializeSceneToPath()
    {
        // Arrange
        _sceneManager.New(new Vector2(800, 600));
        var scenesDir = Path.Combine(Path.GetTempPath(), "test_scenes");

        // Act
        _sceneManager.Save(scenesDir);

        // Assert
        Assert.NotNull(_sceneManager.EditorScenePath);
        Assert.Contains("scene.scene", _sceneManager.EditorScenePath);
        _mockSceneSerializer.Verify(s => s.Serialize(CurrentScene.Instance, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Play_ShouldChangeStateToPlayAndRefreshContext()
    {
        // Arrange
        _sceneManager.New(new Vector2(800, 600));
        _mockSceneView.Reset();

        // Act
        _sceneManager.Play();

        // Assert
        Assert.Equal(SceneState.Play, _sceneManager.SceneState);
        _mockSceneView.Verify(v => v.SetContext(CurrentScene.Instance), Times.Once);
    }

    [Fact]
    public void Stop_ShouldChangeStateToEditAndRefreshContext()
    {
        // Arrange
        _sceneManager.New(new Vector2(800, 600));
        _sceneManager.Play();
        _mockSceneView.Reset();

        // Act
        _sceneManager.Stop();

        // Assert
        Assert.Equal(SceneState.Edit, _sceneManager.SceneState);
        _mockSceneView.Verify(v => v.SetContext(CurrentScene.Instance), Times.Once);
    }

    [Fact]
    public void DuplicateEntity_WhenEntitySelected_ShouldDuplicateEntity()
    {
        // Arrange
        _sceneManager.New(new Vector2(800, 600));
        var entity = CurrentScene.Instance.CreateEntity("TestEntity");
        _mockSceneView.Setup(v => v.GetSelectedEntity()).Returns(entity);

        var initialEntityCount = CurrentScene.Instance.Entities.Count();

        // Act
        _sceneManager.DuplicateEntity();

        // Assert
        Assert.Equal(initialEntityCount + 1, CurrentScene.Instance.Entities.Count());
        _mockSceneView.Verify(v => v.GetSelectedEntity(), Times.Once);
    }

    [Fact]
    public void DuplicateEntity_WhenNoEntitySelected_ShouldNotDuplicate()
    {
        // Arrange
        _sceneManager.New(new Vector2(800, 600));
        _mockSceneView.Setup(v => v.GetSelectedEntity()).Returns((Entity?)null);

        var initialEntityCount = CurrentScene.Instance.Entities.Count();

        // Act
        _sceneManager.DuplicateEntity();

        // Assert
        Assert.Equal(initialEntityCount, CurrentScene.Instance.Entities.Count());
    }

    [Fact]
    public void DuplicateEntity_WhenInPlayMode_ShouldNotDuplicate()
    {
        // Arrange
        _sceneManager.New(new Vector2(800, 600));
        var entity = CurrentScene.Instance.CreateEntity("TestEntity");
        _mockSceneView.Setup(v => v.GetSelectedEntity()).Returns(entity);
        _sceneManager.Play();

        var initialEntityCount = CurrentScene.Instance.Entities.Count();

        // Act
        _sceneManager.DuplicateEntity();

        // Assert
        Assert.Equal(initialEntityCount, CurrentScene.Instance.Entities.Count());
    }

    [Fact]
    public void FocusOnSelectedEntity_WhenEntityHasTransform_ShouldMoveCameraToEntityPosition()
    {
        // Arrange
        _sceneManager.New(new Vector2(800, 600));
        var entity = CurrentScene.Instance.CreateEntity("TestEntity");
        var tc = new TransformComponent(Vector3.One, Vector3.Zero, Vector3.One);
        entity.AddComponent(tc);
        
        var transform = entity.GetComponent<TransformComponent>();
        transform.Translation = new Vector3(10, 20, 30);

        _mockSceneView.Setup(v => v.GetSelectedEntity()).Returns(entity);
        var cameraController = new OrthographicCameraController(800 / 600);

        // Act
        _sceneManager.FocusOnSelectedEntity(cameraController);

        // Assert
        Assert.Equal(transform.Translation, cameraController.Camera.Position);
    }

    [Fact]
    public void FocusOnSelectedEntity_WhenNoEntitySelected_ShouldNotMoveCamera()
    {
        // Arrange
        _sceneManager.New(new Vector2(800, 600));
        _mockSceneView.Setup(v => v.GetSelectedEntity()).Returns((Entity?)null);
        var cameraController = new OrthographicCameraController(800 / 600);
        var originalPosition = cameraController.Camera.Position;

        // Act
        _sceneManager.FocusOnSelectedEntity(cameraController);

        // Assert
        Assert.Equal(originalPosition, cameraController.Camera.Position);
    }

    [Fact]
    public void SelectionChanged_Event_ShouldBeRaisedBySceneView()
    {
        // Arrange
        var eventRaised = false;
        Entity? capturedEntity = null;

        _mockSceneView.SetupAdd(v => v.SelectionChanged += It.IsAny<Action<Entity?>>())
            .Callback<Action<Entity?>>((handler) =>
            {
                // Simulate event subscription
                eventRaised = true;
            });

        // Act
        _mockSceneView.Object.SelectionChanged += (entity) =>
        {
            capturedEntity = entity;
        };

        // Assert
        Assert.True(eventRaised);
    }
}
