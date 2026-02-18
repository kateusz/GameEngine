using System.Numerics;
using Box2D.NetStandard.Collision.Shapes;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Engine.Scene.Systems;
using Serilog;

namespace Engine.Scene;

internal sealed class Scene : IScene
{
    private static readonly ILogger Logger = Log.ForContext<Scene>();

    private readonly IContext _context;
    private readonly IGraphics2D _graphics2D;
    private readonly ITextureFactory _textureFactory;
    private readonly string _path;
    private readonly World _physicsWorld;
    private int _nextEntityId = 1;
    private readonly ISystemManager _systemManager;
    private bool _disposed;
    private readonly List<Entity> _entities = [];

    public Scene(string path, ISceneSystemRegistry systemRegistry, IGraphics2D graphics2D, IContext context, ITextureFactory textureFactory)
    {
        _path = path;
        _graphics2D = graphics2D;
        _context = context;
        _textureFactory = textureFactory;
        _context.Clear();
        
        _systemManager = new SystemManager();

        // Populate system manager from registry (singleton systems shared across scenes)
        systemRegistry.PopulateSystemManager(_systemManager);
        
        _physicsWorld = new World(new Vector2(0, -9.8f));
        var contactListener = new SceneContactListener();
        _physicsWorld.SetContactListener(contactListener);

        // Create and register physics simulation system with the physics world
        // NOTE: This system is per-scene because each scene has its own physics world
        var physicsSimulationSystem = new PhysicsSimulationSystem(_physicsWorld, _context);
        _systemManager.RegisterSystem(physicsSimulationSystem);
    }

    public IEnumerable<Entity> Entities => _entities;

    public Entity CreateEntity(string name)
    {
        var entity = Entity.Create(_nextEntityId++, name);
        _context.Register(entity);
        _entities.Add(entity);

        return entity;
    }

    public void AddEntity(Entity entity)
    {
        if (entity.Id <= 0)
            throw new ArgumentException($"Entity ID must be positive, got {entity.Id}", nameof(entity));

        // Track the highest ID when adding existing entities (e.g., from deserialization)
        if (entity.Id >= _nextEntityId)
            _nextEntityId = entity.Id + 1;

        _context.Register(entity);
        _entities.Add(entity);
    }

    public void DestroyEntity(Entity entity)
    {
        _context.Remove(entity.Id);
        _entities.Remove(entity);
    }

    public void OnRuntimeStart()
    {
        _systemManager.Initialize();

        var view = _context.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var bodyDef = new BodyDef
            {
                position = new Vector2(transform.Translation.X, transform.Translation.Y),
                angle = transform.Rotation.Z,
                type = RigidBody2DTypeToBox2DBody(component.BodyType),
                bullet = component.BodyType == RigidBodyType.Dynamic
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
        _systemManager.Shutdown();
    }

    public void OnUpdateRuntime(TimeSpan ts)
    {
        // Update all systems in priority order:
        // 100: PhysicsSimulationSystem
        // 150: ScriptUpdateSystem
        // 200: SpriteRenderingSystem
        // 205: SubTextureRenderingSystem
        // 210: ModelRenderingSystem
        // 500: PhysicsDebugRenderSystem
        _systemManager.Update(ts);
    }


    /// <summary>
    /// Updates the scene in editor mode (without running physics or scripts).
    /// </summary>
    /// <remarks>
    /// Uses unified Camera interface for rendering in editor mode.
    /// The editor camera (OrthographicCamera) provides both projection and view matrices
    /// through the abstract Camera base class, allowing consistent rendering across
    /// editor and runtime modes.
    /// </remarks>
    public void OnUpdateEditor(TimeSpan ts, Camera camera)
    {
        //TODO: temp disable 3D
        /*
        var baseCamera = camera;
        Matrix4x4 cameraTransform = Matrix4x4.CreateTranslation(camera.Position);

        Renderer3D.Instance.BeginScene(baseCamera, cameraTransform);

        var modelGroup = _context.GetGroup([
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
        
        _graphics2D.BeginScene(camera);

        var spriteGroup = _context.View<SpriteRendererComponent>();
        foreach (var (entity, spriteRendererComponent) in spriteGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            _graphics2D.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
        }

        var subtextureGroup = _context.View<SubTextureRendererComponent>();
        foreach (var (entity, subtextureComponent) in subtextureGroup)
        {
            if (subtextureComponent.Texture is null) 
                continue;

            // Use pre-calculated TexCoords if available (e.g., from animation system)
            // Otherwise calculate from grid coordinates (same as SubTextureRenderingSystem)
            Vector2[] texCoords;
            if (subtextureComponent.TexCoords != null)
            {
                // Direct UV coordinates (used by animation system)
                texCoords = subtextureComponent.TexCoords;
            }
            else
            {
                // Calculate from grid coordinates (traditional subtexture rendering)
                var subTexture = SubTexture2D.CreateFromCoords(
                    subtextureComponent.Texture,
                    subtextureComponent.Coords,
                    subtextureComponent.CellSize,
                    subtextureComponent.SpriteSize
                );
                texCoords = subTexture.TexCoords;
            }

            // Use transform directly without additional scaling (same as runtime)
            var transform = entity.GetComponent<TransformComponent>().GetTransform();
            _graphics2D.DrawQuad(transform, subtextureComponent.Texture, texCoords, entityId: entity.Id);
        }
        
        _graphics2D.EndScene();
    }

    public void OnViewportResize(uint width, uint height)
    {
        Logger.Information("Scene.OnViewportResize called: {Width}x{Height}", width, height);

        var group = _context.View<CameraComponent>();
        foreach (var (entity, cameraComponent) in group)
        {
            if (!cameraComponent.FixedAspectRatio)
            {
                Logger.Information("Updating camera viewport for entity '{EntityName}' to {Width}x{Height}",
                    entity.Name, width, height);
                cameraComponent.Camera.SetViewportSize(width, height);
            }
        }
    }

    public Entity? GetPrimaryCameraEntity()
    {
        var view = _context.View<CameraComponent>();
        foreach (var (entity, component) in view)
        {
            if (component.Primary)
                return entity;
        }

        return null;
    }

    private static BodyType RigidBody2DTypeToBox2DBody(RigidBodyType componentBodyType)
    {
        return componentBodyType switch
        {
            RigidBodyType.Static => BodyType.Static,
            RigidBodyType.Dynamic => BodyType.Dynamic,
            RigidBodyType.Kinematic => BodyType.Kinematic,
            _ => throw new ArgumentOutOfRangeException(nameof(componentBodyType), componentBodyType, null)
        };
    }

    /// <summary>
    /// Duplicates an entity by cloning all of its components.
    /// </summary>
    /// <param name="entity">The entity to duplicate.</param>
    /// <returns>The newly created entity with cloned components.</returns>
    public Entity DuplicateEntity(Entity entity)
    {
        var newEntity = CreateEntity(entity.Name);

        foreach (var component in entity.GetAllComponents())
        {
            newEntity.AddComponentDynamic(component.Clone());
        }

        return newEntity;
    }
    
    /// <summary>
    /// Disposes the scene and cleans up all resources.
    /// Unsubscribes from events, disposes the SystemManager (which handles per-scene systems),
    /// and clears entity storage to prevent memory leaks.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) 
            return;

        Logger.Debug("Disposing scene '{Path}'", _path);

        // Dispose SystemManager which will dispose per-scene systems (PhysicsSimulationSystem)
        // Singleton systems (rendering, scripts) are shared and won't be disposed
        _systemManager?.Dispose();
        
        // Clear entity storage
        _context.Clear();

        _disposed = true;
        Logger.Debug("Scene '{Path}' disposed successfully", _path);
    }
}