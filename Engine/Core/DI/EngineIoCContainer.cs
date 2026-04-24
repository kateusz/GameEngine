using System.Runtime.CompilerServices;
using DryIoc;
using Silk.NET.Assimp;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events;
using Engine.ImGuiNet;
using Engine.Platform.OpenAL;
using Engine.Platform.OpenAL.Effects;
using Engine.Platform.OpenGL;
using Silk.NET.OpenAL;
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
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

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
            made: Made.Of(() => Silk.NET.Windowing.Window.Create(options)),
            setup: Setup.With(preventDisposal: true)
        );
        container.Register<IGameWindowFactory, GameWindowFactory>(Reuse.Singleton);
        container.Register<IGameWindow>(
            made: Made.Of(
                r => ServiceInfo.Of<IGameWindowFactory>(),
                f => f.Create()
            )
        );
        
        container.RegisterDelegate<IContentScaleProvider>(r => r.Resolve<IGameWindow>());
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
        
        RegisterFactories(container);
        
        container.Register<IGraphics2D, Graphics2D>(Reuse.Singleton);
        container.Register<IGraphics3D, Graphics3D>(Reuse.Singleton);
        container.Register<IBloomRenderer, OpenGLBloomRenderer>(Reuse.Singleton);
        container.RegisterDelegate<AL>(_ => AL.GetApi(true), Reuse.Singleton);
        container.RegisterDelegate<ALContext>(_ => ALContext.GetApi(true), Reuse.Singleton);
        container.Register<Engine.Audio.IAudioEngine, OpenALAudioEngine>(Reuse.Singleton);
        container.Register<Engine.Audio.IAudioEffectFactory, OpenALAudioEffectFactory>(Reuse.Singleton);

        // Register SceneSystemRegistry and systems
        container.Register<SceneFactory>(Reuse.Singleton);
        container.Register<RenderingSystemsGroup>(Reuse.Singleton);
        container.Register<ISceneSystemRegistry, SceneSystemRegistry>(Reuse.Singleton);

        // Register ECS systems (all now use dependency injection)
        container.Register<SpriteRenderingSystem>(Reuse.Singleton);
        container.Register<LightingSystem>(Reuse.Singleton);
        container.Register<ModelRenderingSystem>(Reuse.Singleton);
        container.Register<ScriptUpdateSystem>(Reuse.Singleton);
        container.Register<SubTextureRenderingSystem>(Reuse.Singleton);
        container.Register<PhysicsDebugRenderSystem>(Reuse.Singleton);
        container.Register<AudioSystem>(Reuse.Singleton);
        
        container.Register<PrimaryCameraSystem>(Reuse.Singleton);
        container.RegisterMapping<IPrimaryCameraProvider, PrimaryCameraSystem>();
        
        container.Register<ISceneContext, SceneContext>(Reuse.Singleton);
        
        container.Register<SerializerOptions>(Reuse.Singleton);
        container.Register<ComponentDeserializer>(Reuse.Singleton);
        container.Register<IPrefabSerializer, PrefabSerializer>(Reuse.Singleton);
        container.Register<ISceneSerializer, SceneSerializer>(Reuse.Singleton);
    }

    private static void RegisterFactories(Container container)
    {
        container.Register<IRendererApiFactory, RendererApiFactory>(Reuse.Singleton);
        container.Register<ITextureFactory, TextureFactory>(Reuse.Singleton);
        container.Register<IShaderFactory, ShaderFactory>(Reuse.Singleton);
        container.RegisterDelegate<Assimp>(_ => Assimp.GetApi(), Reuse.Singleton);
        container.Register<FbxModelLoader>(Reuse.Singleton);
        container.Register<IMeshFactory, MeshFactory>(Reuse.Singleton);
        container.Register<IVertexBufferFactory, VertexBufferFactory>(Reuse.Singleton);
        container.Register<IIndexBufferFactory, IndexBufferFactory>(Reuse.Singleton);
        container.Register<IFrameBufferFactory, FrameBufferFactory>(Reuse.Singleton);
        container.Register<IVertexArrayFactory, VertexArrayFactory>(Reuse.Singleton);
    }
}