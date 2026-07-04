using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
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
    private readonly string _sceneName;
    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;
    private readonly ITextureFactory? _textureFactory;
    private readonly IContext _context;
    private readonly DebugSettings _debugSettings;
    private readonly ISystemManager _systemManager;
    private readonly PhysicsRuntimeBodyStore _physicsRuntimeBodyStore;
    private readonly PhysicsContactQueue _physicsContactQueue;
    private readonly ScriptRuntimeStore _scriptRuntimeStore;

    internal ScriptRuntimeStore ScriptRuntimeStore => _scriptRuntimeStore;

    public Scene(string path,
        string sceneName,
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        ITextureFactory textureFactory,
        IContext context,
        DebugSettings debugSettings,
        ISystemManager systemManager,
        PhysicsRuntimeBodyStore physicsRuntimeBodyStore,
        PhysicsContactQueue physicsContactQueue,
        ScriptRuntimeStore scriptRuntimeStore)
    {
        _path = path;
        _sceneName = sceneName;
        _graphics2D = graphics2D;
        _graphics3D = graphics3D;
        _textureFactory = textureFactory;
        _context = context;
        _debugSettings = debugSettings;
        _systemManager = systemManager;
        _physicsRuntimeBodyStore = physicsRuntimeBodyStore;
        _physicsContactQueue = physicsContactQueue;
        _scriptRuntimeStore = scriptRuntimeStore;
    }

    public IPhysicsContacts PhysicsContacts => _physicsContactQueue;

    public void RegisterRuntimeSystem(ISystem system) => _systemManager.RegisterSystem(system);

    public IContext Context => _context;

    public string Name => _sceneName;
    public IEnumerable<Entity> Entities => _context.Entities;

    public Entity CreateEntity(string name)
    {
        var entity = Entity.Create(_nextEntityId++, name);
        _context.Register(entity);

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

        // Normalize primary camera flags to ensure at most one primary camera
        if (entity.HasComponent<CameraComponent>() && entity.GetComponent<CameraComponent>().Primary)
            SetPrimaryCamera(entity);
    }

    public void DestroyEntity(Entity entity)
    {
        if (_scriptRuntimeStore.TryGet(entity.Id, out var script))
        {
            try
            {
                script.OnDestroy();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in script OnDestroy for entity '{EntityName}' (ID: {EntityId})", entity.Name, entity.Id);
            }
            _scriptRuntimeStore.Remove(entity.Id);
        }

        _context.Remove(entity.Id);
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

        foreach (var (entity, _) in _context.View<CameraComponent>())
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
        // Update all systems in priority order:
        // 100: PhysicsSimulationSystem
        // 150: ScriptUpdateSystem
        // 200: SpriteRenderingSystem
        // 205: SubTextureRenderingSystem
        // 210: ModelRenderingSystem
        // 500: PhysicsDebugRenderSystem
        _systemManager.Update(ts);
    }


    public void OnUpdateEditor(TimeSpan ts, EditorCamera camera)
    {
        SceneRenderPipeline.RenderScene(
            _context,
            _graphics2D,
            _graphics3D,
            _textureFactory,
            _debugSettings,
            _physicsRuntimeBodyStore,
            SceneRenderPipeline.CameraBinding.FromEditor(camera),
            useTransformFallbackWhenNoBody: true);
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
                cameraComponent.AspectRatio = (float)width / height;
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

    public void SetPrimaryCamera(Entity cameraEntity)
    {
        if (!_context.Contains(cameraEntity.Id))
            throw new ArgumentException("Entity does not belong to this scene", nameof(cameraEntity));

        if (!cameraEntity.HasComponent<CameraComponent>())
            throw new ArgumentException("Entity must have a CameraComponent", nameof(cameraEntity));

        var view = _context.View<CameraComponent>();
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
        _context.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
        Logger.Debug("Scene '{Path}' disposed successfully", _path);
    }
}