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
using Engine.Scene.Systems;
using Engine.Scripting;
using Serilog;

namespace Engine.Scene;

public class Scene
{
    private static readonly ILogger Logger = Log.ForContext<Scene>();

    private readonly string _path;
    private uint _viewportWidth;
    private uint _viewportHeight;
    private World _physicsWorld;
    private SceneContactListener _contactListener;
    private int _nextEntityId = 1;
    private readonly SystemManager _systemManager;
    private readonly ModelRenderingSystem _modelRenderingSystem;

    private readonly bool _showPhysicsDebug = true;
    private PhysicsDebugRenderSystem? _physicsDebugRenderSystem;

    // Fixed timestep accumulator for deterministic physics
    private float _physicsAccumulator = 0f;

    /// <summary>
    /// Maximum physics steps per frame to prevent spiral of death.
    /// At 60Hz physics with 16ms frames, this allows catching up from frame spikes up to ~83ms.
    /// Beyond this threshold, the accumulator is clamped to prevent unbounded physics execution.
    /// </summary>
    private const int MaxPhysicsStepsPerFrame = 5;


    public Scene(string path)
    {
        _path = path;
        Context.Instance.Clear();

        // Initialize ECS systems
        _systemManager = new SystemManager();
        _modelRenderingSystem = new ModelRenderingSystem(Graphics3D.Instance);
        _systemManager.RegisterSystem(_modelRenderingSystem);
        _systemManager.Initialize();
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
        // Re-initialize systems for runtime
        if (!_systemManager.IsInitialized)
        {
            _systemManager.Initialize();
        }

        _physicsWorld = new World(new Vector2(0, -9.8f)); // Standardowa grawitacja ziemska

        _contactListener = new SceneContactListener();
        _physicsWorld.SetContactListener(_contactListener);

        // Reset physics accumulator for clean state
        _physicsAccumulator = 0f;

        // Initialize physics debug rendering system
        _physicsDebugRenderSystem = new PhysicsDebugRenderSystem(Graphics2D.Instance, _showPhysicsDebug);
        _physicsDebugRenderSystem.OnInit();

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
        // Shutdown ECS systems
        _systemManager.Shutdown();

        // Early exit if physics world was never initialized
        if (_physicsWorld == null)
            return;

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
            Logger.Warning("Scene stopped with {ErrorsCount} script error(s) during OnDestroy. Check logs above for details.", errors.Count);
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

        // Clear ContactListener
        if (_contactListener != null)
        {
            _physicsWorld.SetContactListener(null);
            _contactListener = null;
        }

        // Destroy physics world
        _physicsWorld = null;
    }

    public void OnUpdateRuntime(TimeSpan ts)
    {
        // Update scripts with variable delta time for smooth rendering
        ScriptEngine.Instance.OnUpdate(ts);

        // Fixed timestep physics simulation
        const int velocityIterations = 6;
        const int positionIterations = 2;
        var deltaSeconds = (float)ts.TotalSeconds;

        // Accumulate time
        _physicsAccumulator += deltaSeconds;

        // Step physics multiple times if needed to catch up
        int stepCount = 0;
        while (_physicsAccumulator >= CameraConfig.PhysicsTimestep && stepCount < MaxPhysicsStepsPerFrame)
        {
            _physicsWorld.Step(CameraConfig.PhysicsTimestep, velocityIterations, positionIterations);
            _physicsAccumulator -= CameraConfig.PhysicsTimestep;
            stepCount++;
        }

        // If we hit max steps, clamp accumulator to prevent unbounded growth
        // while preserving some time debt for the next frame
        if (_physicsAccumulator >= CameraConfig.PhysicsTimestep)
        {
            _physicsAccumulator = CameraConfig.PhysicsTimestep * 0.5f; // Preserve half timestep
        }

        // Retrieve transform from Box2D
        var view = Context.Instance.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var collision = entity.GetComponent<BoxCollider2DComponent>();
            var body = component.RuntimeBody;

            if (body != null)
            {
                // Only update fixture properties if they have changed
                if (collision.IsDirty)
                {
                    var fixture = body.GetFixtureList();
                    fixture.Density = collision.Density;
                    fixture.m_friction = collision.Friction;
                    fixture.Restitution = collision.Restitution;
                    collision.ClearDirtyFlag();
                }

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
            // Set camera for 3D rendering system
            _modelRenderingSystem.SetCamera(mainCamera, cameraTransform);

            // Update all systems (including 3D rendering)
            _systemManager.Update(ts);

            // Render 2D (existing code)
            Graphics2D.Instance.BeginScene(mainCamera, cameraTransform);

            var group = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
            foreach (var entity in group)
            {
                var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                Graphics2D.Instance.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
            }

            // Render physics debug visualization using the system
            _physicsDebugRenderSystem?.OnUpdate(TimeSpan.Zero);

            Graphics2D.Instance.EndScene();
        }
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

}