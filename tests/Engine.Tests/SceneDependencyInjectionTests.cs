using ECS;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Systems;
using Engine.Scripting;
using NSubstitute;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests;

/// <summary>
/// Demonstrates improved testability after removing singleton dependencies.
/// These tests show how Scene can now be tested with mocked dependencies.
/// </summary>
public class SceneDependencyInjectionTests
{
    
    [Fact]
    public void Scene_CanBeCreated_WithMockedDependencies()
    {
        // Arrange - Create mocks for all dependencies
        var mockContext = Substitute.For<IContext>();
        var mockGraphics2D = Substitute.For<IGraphics2D>();
        var mockSystemRegistry = Substitute.For<ISceneSystemRegistry>();

        // Act - Create scene with injected dependencies (previously impossible with singletons)
        var scene = new EngineScene("test-scene", mockSystemRegistry, mockGraphics2D, mockContext);

        // Assert - Scene was created successfully
        scene.ShouldNotBeNull();
    }

    [Fact]
    public void Scene_CreateEntity_RegistersWithInjectedContext()
    {
        // Arrange
        var mockContext = Substitute.For<IContext>();
        var mockGraphics2D = Substitute.For<IGraphics2D>();
        var mockSystemRegistry = Substitute.For<ISceneSystemRegistry>();

        var scene = new EngineScene("test-scene", mockSystemRegistry, mockGraphics2D, mockContext);

        // Act
        var entity = scene.CreateEntity("TestEntity");

        // Assert - Verify that the entity was registered with the injected context
        mockContext.Received(1).Register(Arg.Is<Entity>(e => e.Name == "TestEntity"));
    }

    [Fact]
    public void Scene_DestroyEntity_RemovesFromInjectedContext()
    {
        // Arrange
        var mockContext = Substitute.For<IContext>();
        var mockGraphics2D = Substitute.For<IGraphics2D>();
        var mockSystemRegistry = Substitute.For<ISceneSystemRegistry>();

        var scene = new EngineScene("test-scene", mockSystemRegistry, mockGraphics2D, mockContext);
        var entity = scene.CreateEntity("TestEntity");

        // Act
        scene.DestroyEntity(entity);

        // Assert - Verify that Remove was called on the injected context
        mockContext.Received(1).Remove(entity.Id);
    }

    [Fact]
    public void SpriteRenderingSystem_CanBeCreated_WithMockedDependencies()
    {
        // Arrange - Create mocks for system dependencies
        var mockRenderer = Substitute.For<IGraphics2D>();
        var mockContext = Substitute.For<IContext>();

        // Act - Create system with injected dependencies (previously used Context.Instance singleton)
        var system = new SpriteRenderingSystem(mockRenderer, mockContext);

        // Assert
        system.ShouldNotBeNull();
        system.Priority.ShouldBe(SystemPriorities.SpriteRenderSystem);
    }

    [Fact]
    public void ScriptUpdateSystem_CanBeCreated_WithMockedScriptEngine()
    {
        // Arrange - Create mock for script engine
        var mockScriptEngine = Substitute.For<IScriptEngine>();

        // Act - Create system with injected script engine (previously used ScriptEngine.Instance)
        var system = new ScriptUpdateSystem(mockScriptEngine);

        // Assert
        system.ShouldNotBeNull();
        system.Priority.ShouldBe(SystemPriorities.ScriptUpdateSystem);
    }

    [Fact]
    public void ScriptUpdateSystem_OnUpdate_CallsScriptEngineOnUpdate()
    {
        // Arrange
        var mockScriptEngine = Substitute.For<IScriptEngine>();
        var system = new ScriptUpdateSystem(mockScriptEngine);
        var deltaTime = TimeSpan.FromSeconds(0.016); // ~60 FPS

        // Act
        system.OnUpdate(deltaTime);

        // Assert - Verify script engine OnUpdate was called with correct delta time
        mockScriptEngine.Received(1).OnUpdate(deltaTime);
    }

    [Fact]
    public void Context_IsNoLongerSingleton_CanCreateMultipleInstances()
    {
        // Demonstrate that Context is no longer a singleton
        // Multiple instances can now be created for isolated testing

        // Act
        var context1 = new Context();
        var context2 = new Context();

        // Assert - These are separate instances, not the same singleton
        context1.ShouldNotBeSameAs(context2);
    }
}
