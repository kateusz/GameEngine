using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Physics;
using Engine.Scene.Systems;
using SceneComponents.Camera;
using Scripting;
using Serilog;

namespace Engine.Scene;

internal sealed class Scene : IScene
{
    private static readonly ILogger Logger = Log.ForContext<Scene>();

    private int _nextEntityId = 1;
    private bool _disposed;
    private readonly string _path;
    private readonly ISystemManager _systemManager;
    private readonly PhysicsContactQueue _physicsContactQueue;
    private readonly IPhysicsWorld2D _physicsWorld;
    private readonly ICameraQueries _cameraQueries;

    internal ScriptRuntimeStore ScriptRuntimeStore { get; }

    public Scene(string path,
        string sceneName,
        IContext context,
        ISystemManager systemManager,
        PhysicsRuntimeBodyStore physicsRuntimeBodyStore,
        PhysicsContactQueue physicsContactQueue,
        ScriptRuntimeStore scriptRuntimeStore,
        IPhysicsWorld2D physicsWorld,
        ICameraQueries cameraQueries)
    {
        _path = path;
        Name = sceneName;
        Context = context;
        _systemManager = systemManager;
        PhysicsBodies = physicsRuntimeBodyStore;
        _physicsContactQueue = physicsContactQueue;
        ScriptRuntimeStore = scriptRuntimeStore;
        _physicsWorld = physicsWorld;
        _cameraQueries = cameraQueries;
    }

    public IPhysicsContacts PhysicsContacts => _physicsContactQueue;

    public IPhysicsQueries PhysicsQueries => _physicsWorld;

    public ICameraQueries CameraQueries => _cameraQueries;

    internal PhysicsRuntimeBodyStore PhysicsBodies { get; }

    public void RegisterRuntimeSystem(ISystem system) => _systemManager.RegisterSystem(system);

    public IContext Context { get; }

    public string Name { get; }

    public Vector4 BackgroundColor { get; set; } = new(0.1f, 0.1f, 0.1f, 1.0f);

    public SceneDimension Dimension { get; set; } = SceneDimension.TwoD;

    public IEnumerable<Entity> Entities => Context.Entities;

    public Entity CreateEntity(string name)
    {
        var entity = Entity.Create(_nextEntityId++, name);
        Context.Register(entity);

        return entity;
    }

    public void AddEntity(Entity entity)
    {
        if (entity.Id <= 0)
            throw new ArgumentException($"Entity ID must be positive, got {entity.Id}", nameof(entity));

        // Track the highest ID when adding existing entities (e.g., from deserialization)
        if (entity.Id >= _nextEntityId)
            _nextEntityId = entity.Id + 1;

        Context.Register(entity);

        // Normalize primary camera flags to ensure at most one primary camera
        if (entity.HasComponent<CameraComponent>() && entity.GetComponent<CameraComponent>().Primary)
            SetPrimaryCamera(entity);
    }

    public void DestroyEntity(Entity entity)
    {
        if (ScriptRuntimeStore.TryGet(entity.Id, out var script))
        {
            try
            {
                script.OnDestroy();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in script OnDestroy for entity '{EntityName}' (ID: {EntityId})", entity.Name, entity.Id);
            }
            ScriptRuntimeStore.Remove(entity.Id);
        }

        Context.Remove(entity.Id);
    }

    public void OnRuntimeStart()
    {
        EnsurePrimaryCamera();
        _systemManager.Initialize();
    }

    private void EnsurePrimaryCamera()
    {
        if (GetPrimaryCameraEntity() is not null)
            return;

        foreach (var (entity, _) in Context.View<CameraComponent>())
        {
            SetPrimaryCamera(entity);
            Logger.Warning(
                "No primary camera was set — '{EntityName}' is now the primary camera. " +
                "Enable Primary on a CameraComponent to choose explicitly.",
                entity.Name);
            return;
        }

        Logger.Warning("Play mode has no camera — nothing will render until you add a CameraComponent.");
    }

    public void OnRuntimeStop() => _systemManager.Shutdown();

    public void OnUpdateRuntime(TimeSpan ts)
    {
        // 100: PhysicsSimulationSystem
        // 110: ScriptUpdateSystem
        // 120: AudioSystem
        // 145: PrimaryCameraSystem
        // 150: SceneRenderSystem
        // 151: PhysicsDebugRenderSystem
        _systemManager.Update(ts);
    }

    public void OnViewportResize(uint width, uint height)
    {
        Logger.Information("Scene.OnViewportResize called: {Width}x{Height}", width, height);

        var group = Context.View<CameraComponent>();
        foreach (var (entity, cameraComponent) in group)
        {
            if (!cameraComponent.FixedAspectRatio)
            {
                Logger.Information("Updating camera viewport for entity '{EntityName}' to {Width}x{Height}",
                    entity.Name, width, height);
                cameraComponent.AspectRatio = (float)width / height;
            }
        }
    }

    public Entity? GetPrimaryCameraEntity()
    {
        var view = Context.View<CameraComponent>();
        foreach (var (entity, component) in view)
        {
            if (component.Primary)
                return entity;
        }

        return null;
    }

    public void SetPrimaryCamera(Entity cameraEntity)
    {
        if (!Context.Contains(cameraEntity.Id))
            throw new ArgumentException("Entity does not belong to this scene", nameof(cameraEntity));

        if (!cameraEntity.HasComponent<CameraComponent>())
            throw new ArgumentException("Entity must have a CameraComponent", nameof(cameraEntity));

        var view = Context.View<CameraComponent>();
        foreach (var (entity, component) in view)
        {
            component.Primary = entity.Id == cameraEntity.Id;
        }
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

        // Normalize primary camera flags to ensure at most one primary camera
        if (newEntity.HasComponent<CameraComponent>() && newEntity.GetComponent<CameraComponent>().Primary)
            SetPrimaryCamera(newEntity);

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

        // Dispose SystemManager which shuts down per-scene systems and physics bodies.
        _systemManager.Dispose();

        // Clear entity storage
        Context.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
        Logger.Debug("Scene '{Path}' disposed successfully", _path);
    }
}