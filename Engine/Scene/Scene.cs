using System.Collections.Concurrent;
using System.Numerics;
using Box2D.NetStandard.Collision.Shapes;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Engine.Scene;

public class Scene
{
    private readonly string _path;
    private uint _viewportWidth;
    private uint _viewportHeight;
    private World _physicsWorld;

    public Scene(string path)
    {
        _path = path;
        Context.Instance.Entities.Clear();
    }

    public ConcurrentBag<Entity> Entities => Context.Instance.Entities;

    public Entity CreateEntity(string name)
    {
        Random random = new Random();
        var randomNumber = random.Next(0, 10001);
        
        var entity = Entity.Create(randomNumber, name);
        entity.OnComponentAdded += OnComponentAdded;
        Context.Instance.Register(entity);

        return entity;
    }

    public void AddEntity(Entity entity) => Context.Instance.Register(entity);

    private void OnComponentAdded(Component component)
    {
        if (component is CameraComponent cameraComponent)
        {
            if (_viewportWidth > 0 && _viewportHeight > 0)
                cameraComponent.Camera.SetViewportSize(_viewportWidth, _viewportHeight);
        }
    }

    public void DestroyEntity(Entity entity)
    {
        var updated = new ConcurrentBag<Entity>(Entities.Where(item => item.Id != entity.Id));
        Context.Instance.Entities = updated;
    }
    
    
    public void OnRuntimeStart()
    {
        _physicsWorld = new World(new Vector2(0, -0.81f));
        var view = Context.Instance.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var bodyDef = new BodyDef
            {
                position = new Vector2(transform.Translation.X, transform.Translation.Y),
                angle = transform.Rotation.Z,
                type = RigidBody2DTypeToBox2DBody(component.BodyType),
                bullet = component.BodyType == RigidBodyType.Dynamic ? true : false
            };

            var body = _physicsWorld.CreateBody(bodyDef);
            body.SetFixedRotation(component.FixedRotation);
            component.RuntimeBody = body;

            if (entity.HasComponent<BoxCollider2DComponent>())
            {
                var boxCollider = entity.GetComponent<BoxCollider2DComponent>();
                var shape = new PolygonShape();
                shape.SetAsBox(boxCollider.Size.X, boxCollider.Size.Y);
                var fixtureDef = new FixtureDef
                {
                    shape = shape,
                    density = boxCollider.Density,
                    friction = boxCollider.Friction,
                    restitution = boxCollider.Restitution
                };

                body.CreateFixture(fixtureDef);
            }
        }
    }

    public void OnRuntimeStop()
    {
        
    }

    public void OnUpdateRuntime(TimeSpan ts)
{
    // Update scripts (existing code)
    var nativeScriptGroup = Context.Instance.View<NativeScriptComponent>();
    
    foreach (var (entity, nativeScriptComponent) in nativeScriptGroup)
    {
        if (nativeScriptComponent.ScriptableEntity.Entity == null)
        {
            nativeScriptComponent.ScriptableEntity.Entity = entity;
            nativeScriptComponent.ScriptableEntity.OnCreate();
        }
        
        nativeScriptComponent.ScriptableEntity.OnUpdate(ts);
    }
    
    // Physics (existing code)
    const int velocityIterations = 6;
    const int positionIterations = 2;
    var deltaSeconds = (float)ts.TotalSeconds;
    deltaSeconds = 1.0f / 60.0f;
    _physicsWorld.Step(deltaSeconds, velocityIterations, positionIterations);
    
    // Retrieve transform from Box2D (existing code)
    var view = Context.Instance.View<RigidBody2DComponent>();
    foreach (var (entity, component) in view)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var collision = entity.GetComponent<BoxCollider2DComponent>();
        var body = component.RuntimeBody;
        
        var fixture = body.GetFixtureList();
        fixture.Density = collision.Density;
        fixture.m_friction = collision.Friction;
        fixture.Restitution = collision.Restitution;
        
        var position = body.GetPosition();
        transform.Translation = new Vector3(position.X, position.Y, 0);
        transform.Rotation = transform.Rotation with { Z = body.GetAngle() };
    }
    
    // Find the main camera
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
        // Render 3D (new code)
        Render3D(mainCamera, cameraTransform);
        
        // Render 2D (existing code)
        Renderer2D.Instance.BeginScene(mainCamera, cameraTransform);
        
        var group = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
        foreach (var entity in group)
        {
            var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();
            Renderer2D.Instance.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
        }
        
        Renderer2D.Instance.EndScene();
    }
}

    public void OnUpdateEditor(TimeSpan ts, EditorCamera camera)
    {
        // First render 3D objects
        Renderer3D.Instance.BeginScene(camera);
    
        var modelGroup = Context.Instance.GetGroup([typeof(TransformComponent), typeof(MeshComponent), typeof(ModelRendererComponent)]);
        foreach (var entity in modelGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var meshComponent = entity.GetComponent<MeshComponent>();
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();
        
            Renderer3D.Instance.DrawModel(transformComponent.GetTransform(), meshComponent, modelRendererComponent, entity.Id);
        }
    
        Renderer3D.Instance.EndScene();
    
        // Then render 2D objects (existing code)
        Renderer2D.Instance.BeginScene(camera);
    
        var spriteGroup = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
        foreach (var entity in spriteGroup)
        {
            var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();
            Renderer2D.Instance.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
        }
    
        Renderer2D.Instance.EndScene();
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

    public Entity? GetPrimaryCameraEntity()
    {
        var view = Context.Instance.View<CameraComponent>();
        foreach (var (entity, component) in view)
        {
            var camera = entity.GetComponent<CameraComponent>();
            if (camera.Primary)
                return entity;
        }

        return null;
    }
    
    private BodyType RigidBody2DTypeToBox2DBody(RigidBodyType componentBodyType)
    {
        return componentBodyType switch
        {
            RigidBodyType.Static => BodyType.Static,
            RigidBodyType.Dynamic => BodyType.Dynamic,
            RigidBodyType.Kinematic => BodyType.Kinematic,
            _ => throw new ArgumentOutOfRangeException(nameof(componentBodyType), componentBodyType, null)
        };
    }

    public void DuplicateEntity(Entity entity)
    {
        var name = entity.Name;
        var newEntity = CreateEntity(name);
        if (entity.HasComponent<TransformComponent>())
        {
            var component = entity.GetComponent<TransformComponent>();
            newEntity.AddComponent(component);
        }

        if (entity.HasComponent<SpriteRendererComponent>())
        {
            var component = entity.GetComponent<SpriteRendererComponent>();
            newEntity.AddComponent(component);
        }
        
        if (entity.HasComponent<SubTextureRendererComponent>())
        {
            var component = entity.GetComponent<SubTextureRendererComponent>();
            newEntity.AddComponent(component);
        }
        
        if (entity.HasComponent<CameraComponent>())
        {
            var component = entity.GetComponent<CameraComponent>();
            newEntity.AddComponent(component);
        }
        
        if (entity.HasComponent<NativeScriptComponent>())
        {
            var component = entity.GetComponent<NativeScriptComponent>();
            newEntity.AddComponent(component);
        }
        
        if (entity.HasComponent<RigidBody2DComponent>())
        {
            var component = entity.GetComponent<RigidBody2DComponent>();
            newEntity.AddComponent(component);
        }
        
        if (entity.HasComponent<BoxCollider2DComponent>())
        {
            var component = entity.GetComponent<BoxCollider2DComponent>();
            newEntity.AddComponent(component);
        }
        
        Context.Instance.Register(newEntity);
    }
    
    public void Render3D(Camera camera, Matrix4x4 cameraTransform)
    {
        Renderer3D.Instance.BeginScene(camera, cameraTransform);
    
        // Get entities with MeshComponent and ModelRendererComponent
        var group = Context.Instance.GetGroup([typeof(TransformComponent), typeof(MeshComponent), typeof(ModelRendererComponent)]);
    
        foreach (var entity in group)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var meshComponent = entity.GetComponent<MeshComponent>();
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();
        
            Renderer3D.Instance.DrawModel(transformComponent.GetTransform(), meshComponent, modelRendererComponent, entity.Id);
        }
    
        Renderer3D.Instance.EndScene();
    }

}