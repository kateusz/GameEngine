using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Renderer;
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
        
        var group = Context.Instance.GetGroup(typeof(TransformComponent), typeof(SpriteRendererComponent));
        
        foreach (var entity in group)
        {
            if (entity.Name != "Player")
                continue;
            
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = transform.Translation with { X = transform.Translation.X + 0.05f };

            if (transform.Translation.X % 5 < 0.05)
            {
                var newBox = _activeScene.CreateEntity("new box");
                var transformComponent = new TransformComponent
                {
                    Translation = transform.Translation with { X = transform.Translation.X + 0.1f, Y = transform.Translation.Y - 5.0f }
                };
                newBox.AddComponent(transformComponent);
                
                var spriteRendererComponent = new SpriteRendererComponent(Vector4.One);
                newBox.AddComponent(spriteRendererComponent);
            }
        }
        
        var camera = Context.Instance.View<CameraComponent>();
        foreach (var (entity, cameraComponent) in camera)
        {
            var translation = entity.GetComponent<TransformComponent>().Translation;
            translation = translation with { X = translation.X + 0.05f };

            entity.GetComponent<TransformComponent>().Translation = translation;
        }
    }
    

    public override void OnImGuiRender()
    {
    }
}