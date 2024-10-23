using System.Numerics;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Components;

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
        
        var entity = _activeScene.CreateEntity("Empty Entity");
        entity.AddComponent<TransformComponent>();
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        RendererCommand.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        RendererCommand.Clear();
        
        _activeScene.OnUpdateRuntime(timeSpan);
    }

    public override void OnImGuiRender()
    {
        // nothing to do 
    }
}