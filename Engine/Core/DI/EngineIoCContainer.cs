using Audio;
using DryIoc;
using ECS;
using Engine.Audio;
using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Platform.OpenAL;
using Engine.Platform.OpenAL.Effects;
using Engine.Platform.SilkNet;
using Silk.NET.OpenAL;
using Engine.Physics;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using Engine.Scripting;
using Input;
using Scripting;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Core.DI;

public static class EngineIoCContainer
{
    public static void RegisterCore(Container container)
    {
        container.Register<IRendererApiConfig>(Reuse.Singleton,
            made: Made.Of(() => new RendererApiConfig(ApiType.SilkNet))
        );
        container.Register<IRendererAPI>(Reuse.Singleton,
            made: Made.Of(
                r => ServiceInfo.Of<IRendererApiFactory>(),
                f => f.Create()
            )
        );
        container.Register<IGraphicsContext, SilkNetGraphicsContext>(Reuse.Singleton);

        container.Register<IScriptEngine, ScriptEngine>(Reuse.Singleton);
        container.Register<IProjectContext, ProjectContext>(Reuse.Singleton);
        container.RegisterInitializer<IProjectContext>((ctx, _) => PathBuilder.UseProjectContext(ctx));
        container.Register<KeyboardInputState>(Reuse.Singleton);
        container.RegisterMapping<IKeyboardInput, KeyboardInputState>();
        container.Register<DebugSettings>(Reuse.Singleton);

        RegisterFactories(container);

        container.Register<IGraphics2D, Graphics2D>(Reuse.Singleton);
        container.Register<IGraphics3D, Graphics3D>(Reuse.Singleton);
        container.Register<HdrTonemapPass>(Reuse.Singleton);
        container.RegisterDelegate<AL>(_ => AL.GetApi(true), Reuse.Singleton);
        container.RegisterDelegate<ALContext>(_ => ALContext.GetApi(true), Reuse.Singleton);
        container.Register<IAudio, OpenALAudioEngine>(Reuse.Singleton);
        container.Register<IAudioEffectFactory, OpenALAudioEffectFactory>(Reuse.Singleton);
        container.Register<AudioPlaybackService>(Reuse.Singleton);
        container.RegisterMapping<IAudioPlayback, AudioPlaybackService>();

        container.Register<SceneFactory>(Reuse.Singleton);
        container.Register<IPhysicsBackendConfig>(Reuse.Singleton,
            made: Made.Of(() => new PhysicsBackendConfig(PhysicsBackendType.Box2D)));
        container.Register<IPhysicsWorld2DFactory, PhysicsWorld2DFactory>(Reuse.Singleton);
        container.Register<ISceneSystemsFactory, SceneSystemsFactory>(Reuse.Singleton);
        container.Register<SystemManagerFactory>(Reuse.Singleton);
        container.RegisterMapping<ISystemManagerFactory, SystemManagerFactory>();

        container.Register<ISceneContext, SceneContext>(Reuse.Singleton);

        container.RegisterDelegate<IContext>(
            r => r.Resolve<ISceneContext>().ActiveScene?.Context
                 ?? throw new InvalidOperationException("Cannot resolve IContext without an active scene."));

        container.RegisterDelegate<IPhysicsContacts>(r =>
            r.Resolve<ISceneContext>().ActiveScene?.PhysicsContacts
            ?? NullPhysicsContacts.Instance);

        container.RegisterDelegate<IPhysicsQueries>(r =>
            r.Resolve<ISceneContext>().ActiveScene?.PhysicsQueries
            ?? NullPhysicsQueries.Instance);

        container.Register<SerializerOptions>(Reuse.Singleton);
        container.Register<ComponentSerializerRegistry>(Reuse.Singleton);
        container.RegisterMapping<IComponentSerializerRegistry, ComponentSerializerRegistry>();
        container.Register<IPrefabSerializer, PrefabSerializer>(Reuse.Singleton);
        container.Register<ISceneSerializer, SceneSerializer>(Reuse.Singleton);
    }

    public static void RegisterWindowing(Container container, EngineHostOptions hostOptions)
    {
        var windowOptions = WindowOptions.Default;
        windowOptions.Size = new Vector2D<int>(hostOptions.WindowWidth, hostOptions.WindowHeight);
        windowOptions.Title = hostOptions.WindowTitle;

        container.Register<IWindow>(Reuse.Singleton,
            made: Made.Of(() => Silk.NET.Windowing.Window.Create(windowOptions)),
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
    }

    private static void RegisterFactories(Container container)
    {
        container.Register<IRendererApiFactory, RendererApiFactory>(Reuse.Singleton);
        container.Register<ITextureFactory, TextureFactory>(Reuse.Singleton);
        container.Register<IShaderFactory, ShaderFactory>(Reuse.Singleton);
        container.Register<IMeshFactory, MeshFactory>(Reuse.Singleton);
        container.Register<IModelFactory, ModelFactory>(Reuse.Singleton);
        container.Register<IVertexBufferFactory, VertexBufferFactory>(Reuse.Singleton);
        container.Register<IIndexBufferFactory, IndexBufferFactory>(Reuse.Singleton);
        container.Register<IFrameBufferFactory, FrameBufferFactory>(Reuse.Singleton);
        container.Register<IVertexArrayFactory, VertexArrayFactory>(Reuse.Singleton);
    }
}
