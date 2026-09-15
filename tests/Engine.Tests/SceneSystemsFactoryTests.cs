using Audio;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using Shouldly;

namespace Engine.Tests;

public class SceneSystemsFactoryTests
{
    [Fact]
    public void Populate_Registers2DStepperAndDebug()
    {
        var registered = Populate();

        try
        {
            registered.ShouldContain(s => s is PhysicsSimulationSystem);
            registered.ShouldContain(s => s is PhysicsDebugRenderSystem);
        }
        finally
        {
            foreach (var system in registered)
                (system as IDisposable)?.Dispose();
        }
    }

    private static IReadOnlyList<ISystem> Populate()
    {
        var factory = new SceneSystemsFactory(
            Substitute.For<IGraphics2D>(),
            Substitute.For<IGraphics3D>(),
            Substitute.For<ITextureFactory>(),
            new DebugSettings(),
            Substitute.For<IAudio>(),
            new AudioPlaybackService(),
            Substitute.For<IModelFactory>());

        var systemManager = new SystemManager();
        factory.PopulateSystemManager(
            systemManager,
            new Context(),
            new PhysicsRuntimeBodyStore(),
            new PhysicsContactQueue());

        return systemManager.Systems;
    }
}
