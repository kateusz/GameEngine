using System.Numerics;
using Engine.Core;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;

namespace FlappyBird;

public class GameLayer : Layer
{
    private Scene _activeScene;

    public GameLayer(string name) : base(name)
    {
    }

    public override void OnAttach()
    {
        _activeScene = new Scene();
        SceneSerializer.Deserialize(_activeScene, "assets/scenes/Example.scene");
        _activeScene.OnRuntimeStart();
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();
        
        _activeScene.OnUpdateRuntime(timeSpan);
    }

    public override void OnImGuiRender()
    {
    }
}