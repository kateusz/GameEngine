using System.Runtime.CompilerServices;
using DryIoc;
using Engine.Animation;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events;
using Engine.ImGuiNet;
using Engine.Platform.OpenAL;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using Engine.Scripting;
using Silk.NET.Maths;
using Silk.NET.Windowing;

[assembly: InternalsVisibleTo("Engine.Tests")]

namespace Engine.Core.DI;

public static class EngineIoCContainer
{
    public static void Register(Container container)
    {
        var props = new WindowProps("Editor", (int)DisplayConfig.DefaultWindowWidth, (int)DisplayConfig.DefaultWindowHeight);
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(props.Width, props.Height);
        options.Title = "Game Window";
            
        container.Register<IRendererApiConfig>(Reuse.Singleton,
            made: Made.Of(() => new RendererApiConfig(ApiType.SilkNet))
        );
        container.Register<IRendererAPI>(Reuse.Singleton,
            made: Made.Of(
                r => ServiceInfo.Of<IRendererApiFactory>(),
                f => f.Create()
            )
        );
        container.Register<IWindow>(Reuse.Singleton,
            made: Made.Of(() => Silk.NET.Windowing.Window.Create(options))
        );
        container.Register<IGameWindowFactory, GameWindowFactory>(Reuse.Singleton);
        container.Register<IGameWindow>(
            made: Made.Of(
                r => ServiceInfo.Of<IGameWindowFactory>(),
                f => f.Create()
            )
        );
        
        container.Register<IInputSystemFactory, InputSystemFactory>(Reuse.Singleton);

        container.Register<IImGuiLayerFactory, ImGuiLayerFactory>(Reuse.Singleton);
        container.Register<IImGuiLayer>(
            made: Made.Of(
                r => ServiceInfo.Of<IImGuiLayerFactory>(),
                f => f.Create()
            )
        );
        
        container.Register<EventBus, EventBus>(Reuse.Singleton);
        container.Register<IScriptEngine, ScriptEngine>(Reuse.Singleton);
        container.Register<DebugSettings>(Reuse.Singleton);
        container.Register<IAssetsManager, AssetsManager>(Reuse.Singleton);
        
        RegisterFactories(container);
        
        container.Register<IGraphics2D, Graphics2D>(Reuse.Singleton);
        container.Register<IGraphics3D, Graphics3D>(Reuse.Singleton);
        container.Register<Engine.Audio.IAudioEngine, OpenALAudioEngine>(Reuse.Singleton);

        // Register SceneSystemRegistry and systems
        container.Register<SceneFactory>(Reuse.Singleton);
        container.Register<ISceneSystemRegistry, SceneSystemRegistry>(Reuse.Singleton);

        // Register ECS systems (all now use dependency injection)
        container.Register<SpriteRenderingSystem>(Reuse.Singleton);
        container.Register<ModelRenderingSystem>(Reuse.Singleton);
        container.Register<ScriptUpdateSystem>(Reuse.Singleton);
        container.Register<SubTextureRenderingSystem>(Reuse.Singleton);
        container.Register<PhysicsDebugRenderSystem>(Reuse.Singleton);
        container.Register<AudioSystem>(Reuse.Singleton);
        container.Register<TileMapRenderSystem>(Reuse.Singleton);
        
        container.Register<IAnimationAssetManager, AnimationAssetManager>(Reuse.Singleton);
        container.Register<AnimationSystem>(Reuse.Singleton);
        
        container.Register<ISceneContext, SceneContext>(Reuse.Singleton);
        
        container.Register<IPrefabSerializer, PrefabSerializer>(Reuse.Singleton);
        container.Register<ISceneSerializer, SceneSerializer>(Reuse.Singleton);
    }

    private static void RegisterFactories(Container container)
    {
        container.Register<IRendererApiFactory, RendererApiFactory>(Reuse.Singleton);
        container.Register<ITextureFactory, TextureFactory>(Reuse.Singleton);
        container.Register<IShaderFactory, ShaderFactory>(Reuse.Singleton);
        container.Register<IMeshFactory, MeshFactory>(Reuse.Singleton);
        container.Register<IVertexBufferFactory, VertexBufferFactory>(Reuse.Singleton);
        container.Register<IIndexBufferFactory, IndexBufferFactory>(Reuse.Singleton);
        container.Register<IFrameBufferFactory, FrameBufferFactory>(Reuse.Singleton);
        container.Register<IVertexArrayFactory, VertexArrayFactory>(Reuse.Singleton);
    }
}