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
using Engine.Scripting;
using NLog;

namespace Engine.Scene;

public class Scene
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
    private readonly string _path;
    private uint _viewportWidth;
    private uint _viewportHeight;
    private World _physicsWorld;
    private SceneContactListener _contactListener;

    private readonly bool _showPhysicsDebug = true;


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

    private void OnComponentAdded(IComponent component)
    {
        if (component is CameraComponent cameraComponent)
        {
            if (_viewportWidth > 0 && _viewportHeight > 0)
                cameraComponent.Camera.SetViewportSize(_viewportWidth, _viewportHeight);
        }
    }

    public void DestroyEntity(Entity entity)
    {
        var entitiesToKeep = new List<Entity>();
        foreach (var existingEntity in Entities)
        {
            if (existingEntity.Id != entity.Id)
            {
                entitiesToKeep.Add(existingEntity);
            }
        }

        var updated = new ConcurrentBag<Entity>(entitiesToKeep);
        Context.Instance.Entities = updated;
    }

    public void OnRuntimeStart()
    {
        _physicsWorld = new World(new Vector2(0, 0)); //-0.81f

        _contactListener = new SceneContactListener();
        _physicsWorld.SetContactListener(_contactListener);

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

            body.SetUserData(entity);

            component.RuntimeBody = body;

            if (entity.HasComponent<BoxCollider2DComponent>())
            {
                var boxCollider = entity.GetComponent<BoxCollider2DComponent>();
                var shape = new PolygonShape();
                
                var actualSizeX = boxCollider.Size.X * transform.Scale.X;
                var actualSizeY = boxCollider.Size.Y * transform.Scale.Y;
                var actualOffsetX = boxCollider.Offset.X * transform.Scale.X;
                var actualOffsetY = boxCollider.Offset.Y * transform.Scale.Y;
    
                var center = new Vector2(actualOffsetX, actualOffsetY);
                shape.SetAsBox(actualSizeX / 2.0f, actualSizeY / 2.0f, center, 0.0f);
                
                var fixtureDef = new FixtureDef
                {
                    shape = shape,
                    density = boxCollider.Density,
                    friction = boxCollider.Friction,
                    restitution = boxCollider.Restitution,
                    isSensor = boxCollider.IsTrigger
                };

                body.CreateFixture(fixtureDef);
            }
        }
    }

    public void OnRuntimeStop()
    {
        // First, mark all script entities as "stopping" to prevent new physics operations
        var scriptEntities = Context.Instance.View<NativeScriptComponent>();
        foreach (var (entity, component) in scriptEntities)
        {
            if (component.ScriptableEntity != null)
            {
                try
                {
                    component.ScriptableEntity.OnDestroy();
                }
                catch (Exception ex)
                {
                    // Log but don't crash
                    Logger.Error(ex, "Error in script OnDestroy");
                }
            }
        }

        // Clear ContactListener
        if (_physicsWorld != null && _contactListener != null)
        {
            _physicsWorld.SetContactListener(null);
            _contactListener = null;
        }

        // Clear UserData from bodies
        var view = Context.Instance.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            if (component.RuntimeBody != null)
            {
                component.RuntimeBody.SetUserData(null);
                component.RuntimeBody = null;
            }
        }

        // Destroy physics world
        _physicsWorld = null;
    }

    public void OnUpdateRuntime(TimeSpan ts)
    {
        // Update scripts (existing code)
        ScriptEngine.Instance.OnUpdate(ts);

        // Physics (existing code)
        const int velocityIterations = 6;
        const int positionIterations = 2;
        var deltaSeconds = (float)ts.TotalSeconds;
        deltaSeconds = CameraConfig.PhysicsTimestep;
        _physicsWorld.Step(deltaSeconds, velocityIterations, positionIterations);

        // Retrieve transform from Box2D (existing code)
        var view = Context.Instance.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var collision = entity.GetComponent<BoxCollider2DComponent>();
            var body = component.RuntimeBody;

            if (body != null)
            {
                var fixture = body.GetFixtureList();
                fixture.Density = collision.Density;
                fixture.m_friction = collision.Friction;
                fixture.Restitution = collision.Restitution;

                var position = body.GetPosition();
                transform.Translation = new Vector3(position.X, position.Y, 0);
                transform.Rotation = transform.Rotation with { Z = body.GetAngle() };
            }
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
            Graphics2D.Instance.BeginScene(mainCamera, cameraTransform);

            var group = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
            foreach (var entity in group)
            {
                var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                Graphics2D.Instance.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
            }

            if (_showPhysicsDebug)
            {
                // todo: decorator
                DrawPhysicsDebugSimple();
            }
            
            Graphics2D.Instance.EndScene();
        }
    }

    private void DrawPhysicsDebugSimple()
    {
        var rigidBodyView = Context.Instance.View<RigidBody2DComponent>();
        foreach (var (entity, rigidBodyComponent) in rigidBodyView)
        {
            if (rigidBodyComponent.RuntimeBody == null) 
                continue;
            
            // Pobierz pozycję z Box2D body
            var bodyPosition = rigidBodyComponent.RuntimeBody.GetPosition();

            // Rysuj BoxCollider2D jeśli istnieje
            if (entity.HasComponent<BoxCollider2DComponent>())
            {
                var boxCollider = entity.GetComponent<BoxCollider2DComponent>();
                var transform = entity.GetComponent<TransformComponent>();
                var color = GetBodyDebugColor(rigidBodyComponent.RuntimeBody);

                var position = new Vector3(bodyPosition.X, bodyPosition.Y, 0.0f);
                
                // Box2D używa half-extents
                var size = new Vector2(
                    boxCollider.Size.X * 2.0f * transform.Scale.X,
                    boxCollider.Size.Y * 2.0f * transform.Scale.Y
                );

                // Używa Twojego istniejącego Renderer2D.DrawRect
                Graphics2D.Instance.DrawRect(position, size, color, entity.Id);
            }
        }
    }

    private static Vector4 GetBodyDebugColor(Body body)
    {
        if (!body.IsEnabled())
            return new Vector4(0.5f, 0.5f, 0.3f, 1.0f); // Nieaktywne

        return body.Type() switch
        {
            BodyType.Static => new Vector4(0.5f, 0.9f, 0.5f, 1.0f) // Zielone
            ,
            BodyType.Kinematic => new Vector4(0.5f, 0.5f, 0.9f, 1.0f) // Niebieskie
            ,
            _ => body.IsAwake()
                ? new Vector4(0.9f, 0.7f, 0.7f, 1.0f) // Różowe (aktywne)
                : new Vector4(0.6f, 0.6f, 0.6f, 1.0f)
        };
    }

    public void OnUpdateEditor(TimeSpan ts, OrthographicCamera camera)
    {
        //TODO: temp disable 3D
        /*
        var baseCamera = camera;
        Matrix4x4 cameraTransform = Matrix4x4.CreateTranslation(camera.Position);

        Renderer3D.Instance.BeginScene(baseCamera, cameraTransform);

        var modelGroup = Context.Instance.GetGroup([
            typeof(TransformComponent), typeof(MeshComponent), typeof(ModelRendererComponent)
        ]);
        foreach (var entity in modelGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var meshComponent = entity.GetComponent<MeshComponent>();
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();

            Renderer3D.Instance.DrawModel(transformComponent.GetTransform(), meshComponent, modelRendererComponent,
                entity.Id);
        }

        Renderer3D.Instance.EndScene();
        */

        // Then render 2D objects using the orthographic camera directly
        Graphics2D.Instance.BeginScene(camera);

        var spriteGroup = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
        foreach (var entity in spriteGroup)
        {
            var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();
            Graphics2D.Instance.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
        }

        Graphics2D.Instance.EndScene();
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
    }

    public void Render3D(Camera camera, Matrix4x4 cameraTransform)
    {
        Graphics3D.Instance.BeginScene(camera, cameraTransform);

        // Get entities with MeshComponent and ModelRendererComponent
        var group = Context.Instance.GetGroup([
            typeof(TransformComponent), typeof(MeshComponent), typeof(ModelRendererComponent)
        ]);

        foreach (var entity in group)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var meshComponent = entity.GetComponent<MeshComponent>();
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();

            Graphics3D.Instance.DrawModel(transformComponent.GetTransform(), meshComponent, modelRendererComponent,
                entity.Id);
        }

        Graphics3D.Instance.EndScene();
    }
}