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

    // todo: assign entities to Scene
    public Entity CreateEntity(string name)
    {
        var entity = new Entity(name);
        entity.OnComponentAdded += OnComponentAdded;
        return entity;
    }

    private void OnComponentAdded(Component component)
    {
        if (component is CameraComponent cameraComponent)
        {
            cameraComponent.Camera.SetViewportSize(_viewportWidth, _viewportHeight);
        }
    }

    public void DestroyEntity(Entity entity)
    {
        Context.Instance.Entities.Remove(entity);
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
                nativeScriptComponent.ScriptableEntity.OnCreate();
            }
            
            nativeScriptComponent.ScriptableEntity.OnUpdate(ts);
        }
        
        // Render 2D
        Camera? mainCamera = null;
        var cameraGroup = Context.Instance.GetGroup([typeof(TransformComponent), typeof(CameraComponent)]);
        
        var cameraTransform = Matrix4x4.Identity;
        
        foreach (var entity in cameraGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var cameraComponent = entity.GetComponent<CameraComponent>();

            if (cameraComponent.Primary)
            {
                mainCamera = cameraComponent.Camera;
                cameraTransform = transformComponent.GetTransform();
                break;
            }
        }

        if (mainCamera != null)
        {
            Renderer2D.Instance.BeginScene(mainCamera, cameraTransform);
            
            var group = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
            foreach (var entity in group)
            {
                var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                Renderer2D.Instance.DrawQuad(transformComponent.GetTransform(), spriteRendererComponent.Color);
            }
            
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