using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Engine.Scene;

public class Scene
{
    private uint _viewportWidth;
    private uint _viewportHeight;

    public Entity CreateEntity(string name)
    {
        var entity = new Entity(name);
        return entity;
    }

    public void OnUpdate(TimeSpan ts)
    {
        // Update scripts
        var nativeScriptGroup = Context.Instance.View<NativeScriptComponent>();
        
        foreach (var (entity, nativeScriptComponent) in nativeScriptGroup)
        {

            if (nativeScriptComponent.ScriptableEntity.Entity == null)
            {
                //todo: loop ref?
                nativeScriptComponent.ScriptableEntity.Entity = entity;
            }
            
            nativeScriptComponent.ScriptableEntity.OnUpdate(ts);
        }
        
        // Render 2D
        var cameraGroup = Context.Instance.GetGroup([typeof(TransformComponent), typeof(CameraComponent)]);
        
        Camera? camera = null;
        var cameraTransform = Matrix4x4.Identity;
        
        foreach (var entity in cameraGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var cameraComponent = entity.GetComponent<CameraComponent>();

            if (cameraComponent.Primary)
            {
                camera = cameraComponent.Camera;
                cameraTransform = transformComponent.Transform;
            }
        }
        
        var group = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
        foreach (var entity in group)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
            
            Renderer2D.Instance.BeginScene(camera, cameraTransform);
            Renderer2D.Instance.DrawQuad(transformComponent.Transform, spriteRendererComponent.Color);
            Renderer2D.Instance.EndScene();
        }
    }

    public void OnViewportResize(uint width, uint height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
        
        var group = Context.Instance.GetGroup([typeof(CameraComponent)]);
        foreach (var entity in group)
        {
            var cameraComponent = entity.GetComponent<CameraComponent>();
            if (!cameraComponent.FixedAspectRatio)
            {
                cameraComponent.Camera.SetViewportSize(width, height);
            }
        }
    }
}