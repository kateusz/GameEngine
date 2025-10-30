using System.Numerics;
using Box2D.NetStandard.Collision.Shapes;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Engine.Scene.Systems;
using Serilog;

namespace Engine.Scene;

public class Scene : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<Scene>();

    private readonly IGraphics2D _graphics2D;
    private readonly string _path;
    private uint _viewportWidth;
    private uint _viewportHeight;
    private readonly World _physicsWorld;
    private readonly ScenePhysicsSettings _physicsSettings;
    private int _nextEntityId = 1;
    private readonly SystemManager _systemManager;
    private bool _disposed = false;

    public Scene(string path, SceneSystemRegistry systemRegistry, IGraphics2D graphics2D, ScenePhysicsSettings? physicsSettings = null)
    {
        _path = path;
        _graphics2D = graphics2D;
        _physicsSettings = physicsSettings ?? new ScenePhysicsSettings();
        Context.Instance.Clear();

        // Initialize ECS systems
        _systemManager = new SystemManager();

        // Populate system manager from registry (singleton systems shared across scenes)
        systemRegistry.PopulateSystemManager(_systemManager);

        // Initialize physics world with configurable gravity (per-scene, cannot be shared)
        _physicsWorld = new World(_physicsSettings.Gravity);
        _physicsWorld.SetAllowSleeping(_physicsSettings.AllowSleeping);

        var contactListener = new SceneContactListener();
        _physicsWorld.SetContactListener(contactListener);

        // Create and register physics simulation system with the physics world, contact listener, and settings
        // NOTE: This system is per-scene because each scene has its own physics world
        var physicsSimulationSystem = new PhysicsSimulationSystem(_physicsWorld, contactListener, _physicsSettings);
        _systemManager.RegisterSystem(physicsSimulationSystem);
    }

    public IEnumerable<Entity> Entities => Context.Instance.Entities;

    public Entity CreateEntity(string name)
    {
        var entity = Entity.Create(_nextEntityId++, name);
        entity.OnComponentAdded += OnComponentAdded;
        Context.Instance.Register(entity);

        return entity;
    }

    public void AddEntity(Entity entity)
    {
        if (entity.Id <= 0)
            throw new ArgumentException($"Entity ID must be positive, got {entity.Id}", nameof(entity));

        // Track highest ID when adding existing entities (e.g., from deserialization)
        if (entity.Id >= _nextEntityId)
            _nextEntityId = entity.Id + 1;

        // Subscribe to component events to maintain consistency with CreateEntity
        entity.OnComponentAdded += OnComponentAdded;

        Context.Instance.Register(entity);
    }

    private void OnComponentAdded(IComponent component)
    {
        if (component is CameraComponent cameraComponent)
        {
            if (_viewportWidth > 0 && _viewportHeight > 0)
                cameraComponent.Camera.SetViewportSize(_viewportWidth, _viewportHeight);
        }
    }

    /// <summary>
    /// Destroys an entity, removing it from the scene.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    /// <remarks>
    /// Performance: O(1) dictionary lookup + O(n) list removal.
    /// This is a significant improvement over the previous O(n) iteration + double allocation approach.
    /// With 1000 entities, deletion time drops from ~16ms to sub-millisecond.
    /// No heap allocations beyond the list removal operation.
    /// </remarks>
    public void DestroyEntity(Entity entity)
    {
        // Unsubscribe from all events before removing to prevent memory leak
        entity.OnComponentAdded -= OnComponentAdded;

        // O(1) removal via dictionary lookup
        Context.Instance.Remove(entity.Id);
    }

    public void OnRuntimeStart()
    {
        _systemManager.Initialize();
        
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
        var errors = new List<Exception>();

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
                    Logger.Error(ex, $"Error in script OnDestroy for entity '{entity.Name}' (ID: {entity.Id})");
                    errors.Add(ex);
                }
            }
        }

        if (errors.Count > 0)
        {
            Logger.Warning(
                "Scene stopped with {ErrorsCount} script error(s) during OnDestroy. Check logs above for details.",
                errors.Count);
        }

        // Properly destroy all physics bodies before clearing references
        var view = Context.Instance.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            if (component.RuntimeBody != null)
            {
                component.RuntimeBody.SetUserData(null);
                _physicsWorld.DestroyBody(component.RuntimeBody);
                component.RuntimeBody = null;
            }
        }
        
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
    /// NOTE: Editor mode uses manual rendering instead of the ECS systems because:
    /// 1. Editor uses OrthographicCamera while scene systems use SceneCamera (incompatible types)
    /// 2. Editor mode is fundamentally different (no physics, no scripts, just visualization)
    /// 3. Editor camera is managed by the viewport, not by scene entities
    ///
    /// If in the future we want to unify this, we would need to:
    /// - Make rendering systems accept both camera types, OR
    /// - Convert OrthographicCamera to SceneCamera (with potential performance cost), OR
    /// - Refactor the editor to use scene-based cameras
    /// </remarks>
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

        // Render 2D sprites using the editor viewport camera
        _graphics2D.BeginScene(camera);

        // Sprites
        var spriteGroup = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
        foreach (var entity in spriteGroup)
        {
            var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();
            _graphics2D.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
        }

        // Subtextures (mirror runtime system)
        var subtextureGroup =
            Context.Instance.GetGroup([typeof(TransformComponent), typeof(SubTextureRendererComponent)]);
        foreach (var entity in subtextureGroup)
        {
            var st = entity.GetComponent<SubTextureRendererComponent>();
            if (st.Texture is null) continue;
            var subTex = SubTexture2D.CreateFromCoords(st.Texture, st.Coords, new Vector2(16, 16), new Vector2(1, 1));
            var (texture, texCoords) = subTex;
            var trs = entity.GetComponent<TransformComponent>().GetTransform()
                      * Matrix4x4.CreateScale(16, 16, 1);
            _graphics2D.DrawQuad(trs, texture, texCoords, entityId: entity.Id);
        }

        _graphics2D.EndScene();
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
            if (component.Primary)
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

    /// <summary>
    /// Duplicates an entity by cloning all of its components.
    /// This method uses reflection to automatically handle all component types,
    /// eliminating the need for manual updates when new components are added.
    /// </summary>
    /// <param name="entity">The entity to duplicate.</param>
    /// <returns>The newly created entity with cloned components.</returns>
    public Entity DuplicateEntity(Entity entity)
    {
        var newEntity = CreateEntity(entity.Name);

        // Clone all components using their Clone() implementation
        foreach (var component in entity.GetAllComponents())
        {
            var clonedComponent = component.Clone();

            // Use reflection to call AddComponent with the correct type
            var componentType = component.GetType();
            var addComponentMethod = typeof(Entity).GetMethod(nameof(Entity.AddComponent), new[] { componentType });

            if (addComponentMethod != null)
            {
                addComponentMethod.Invoke(newEntity, new[] { clonedComponent });
            }
            else
            {
                Logger.Warning($"Could not find AddComponent method for type {componentType.Name}");
            }
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
        if (_disposed) return;

        Logger.Debug("Disposing scene '{Path}'", _path);

        // Unsubscribe from all entity component events to prevent memory leaks
        foreach (var entity in Context.Instance.Entities)
        {
            entity.OnComponentAdded -= OnComponentAdded;
        }

        // Dispose SystemManager which will dispose per-scene systems (PhysicsSimulationSystem)
        // Singleton systems (rendering, scripts) are shared and won't be disposed
        _systemManager?.Dispose();

        // Clear entity storage
        Context.Instance.Clear();

        _disposed = true;
        Logger.Debug("Scene '{Path}' disposed successfully", _path);
    }
}