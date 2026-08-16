using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Physics;
using Engine.Scene.Systems;
using SceneComponents;
using SceneComponents.Camera;
using Scripting;
using Serilog;

namespace Engine.Scene;

internal sealed class Scene : IScene
{
    private static readonly ILogger Logger = Log.ForContext<Scene>();
    private static readonly IReadOnlyList<Entity> EmptyEntities = Array.Empty<Entity>();

    private int _nextEntityId = 1;
    private bool _disposed;
    private readonly string _path;
    private readonly ISystemManager _systemManager;
    private readonly PhysicsContactQueue _physicsContactQueue;
    private readonly IPhysicsQueries _physicsQueries;
    private readonly ICameraQueries _cameraQueries;

    // parent Id → ordered child entities (insertion order). Roots are entities with no ParentComponent / null ParentId.
    private readonly Dictionary<int, List<Entity>> _childrenIndex = new();

    internal ScriptRuntimeStore ScriptRuntimeStore { get; }

    public Scene(string path,
        string sceneName,
        IContext context,
        ISystemManager systemManager,
        PhysicsRuntimeBodyStore physicsRuntimeBodyStore,
        PhysicsContactQueue physicsContactQueue,
        ScriptRuntimeStore scriptRuntimeStore,
        IPhysicsQueries physicsQueries,
        ICameraQueries cameraQueries)
    {
        _path = path;
        Name = sceneName;
        Context = context;
        _systemManager = systemManager;
        PhysicsBodies = physicsRuntimeBodyStore;
        _physicsContactQueue = physicsContactQueue;
        ScriptRuntimeStore = scriptRuntimeStore;
        _physicsQueries = physicsQueries;
        _cameraQueries = cameraQueries;

        // After scripts (110), before audio (120) — locals settle first, then world caches.
        _systemManager.RegisterSystem(new TransformHierarchySystem(UpdateWorldTransforms));
    }

    public IPhysicsContacts PhysicsContacts => _physicsContactQueue;

    public IPhysicsQueries PhysicsQueries => _physicsQueries;

    public ICameraQueries CameraQueries => _cameraQueries;

    internal PhysicsRuntimeBodyStore PhysicsBodies { get; }

    public void RegisterRuntimeSystem(ISystem system) => _systemManager.RegisterSystem(system);

    public IContext Context { get; }

    public string Name { get; }

    public Vector4 BackgroundColor { get; set; } = new(0.1f, 0.1f, 0.1f, 1.0f);

    public ScenePostProcessSettings PostProcess { get; set; } = new();

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
        if (!Context.Contains(entity.Id))
            return;

        // Snapshot children first — recursive destroy mutates the index
        foreach (var child in GetChildren(entity).ToList())
            DestroyEntity(child);

        DestroyEntityLeaf(entity);
    }

    private void DestroyEntityLeaf(Entity entity)
    {
        DetachFromParentIndex(entity);
        _childrenIndex.Remove(entity.Id);

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
        // 115: TransformHierarchySystem (world caches)
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

    public Entity DuplicateEntity(Entity entity)
    {
        if (!Context.Contains(entity.Id))
            throw new ArgumentException("Entity does not belong to this scene", nameof(entity));

        var toClone = CollectSubtree(entity);
        var idMap = new Dictionary<int, int>(toClone.Count);

        Entity? rootClone = null;
        foreach (var source in toClone)
        {
            var clone = CreateEntity(source.Name);
            idMap[source.Id] = clone.Id;

            foreach (var component in source.GetAllComponents())
                clone.AddComponentDynamic(component.Clone());

            if (rootClone is null)
                rootClone = clone;
        }

        foreach (var (_, newId) in idMap)
        {
            var clone = Context.GetById(newId);
            if (!clone.TryGetComponent<ParentComponent>(out var parentComp) || parentComp.ParentId is not int oldParentId)
                continue;

            if (idMap.TryGetValue(oldParentId, out var mappedParentId))
                parentComp.ParentId = mappedParentId;
            // else keep ParentId — duplicate stays under the same external parent
        }

        RebuildHierarchyIndex();

        // Normalize primary camera flags to ensure at most one primary camera
        if (rootClone!.HasComponent<CameraComponent>() && rootClone.GetComponent<CameraComponent>().Primary)
            SetPrimaryCamera(rootClone);

        return rootClone;
    }

    public Entity? GetParent(Entity entity)
    {
        if (!entity.TryGetComponent<ParentComponent>(out var parent) || parent.ParentId is not int parentId)
            return null;

        return Context.Contains(parentId) ? Context.GetById(parentId) : null;
    }

    /// <summary>
    /// Direct children of <paramref name="entity"/>. Returns a snapshot detached from the
    /// live hierarchy index so callers can iterate safely during mutations.
    /// </summary>
    public IReadOnlyList<Entity> GetChildren(Entity entity)
    {
        if (!_childrenIndex.TryGetValue(entity.Id, out var children) || children.Count == 0)
            return EmptyEntities;

        return children.ToArray();
    }

    public IReadOnlyList<Entity> GetRootEntities()
    {
        var roots = new List<Entity>();
        foreach (var entity in Context.Entities)
        {
            if (!entity.TryGetComponent<ParentComponent>(out var parent) || parent.ParentId is null)
                roots.Add(entity);
        }

        return roots;
    }

    public bool SetParent(Entity child, Entity? parent)
    {
        if (!Context.Contains(child.Id))
            return false;

        if (parent is not null)
        {
            if (!Context.Contains(parent.Id))
                return false;

            if (parent.Id == child.Id)
                return false;

            // Cycle: walking ancestors of new parent must not hit child
            for (var ancestor = parent; ancestor is not null; ancestor = GetParent(ancestor))
            {
                if (ancestor.Id == child.Id)
                    return false;
            }
        }

        // Same parent → no-op (preserves sibling order)
        var currentParent = GetParent(child);
        if (currentParent is null && parent is null)
            return true;
        if (currentParent is not null && parent is not null && currentParent.Id == parent.Id)
            return true;

        DetachFromParentIndex(child);

        if (parent is null)
        {
            if (child.HasComponent<ParentComponent>())
                child.RemoveComponent<ParentComponent>();
        }
        else
        {
            if (!child.TryGetComponent<ParentComponent>(out var parentComp))
            {
                parentComp = new ParentComponent(parent.Id);
                child.AddComponent(parentComp);
            }
            else
            {
                parentComp.ParentId = parent.Id;
            }

            if (!_childrenIndex.TryGetValue(parent.Id, out var list))
            {
                list = [];
                _childrenIndex[parent.Id] = list;
            }

            list.Add(child);
        }

        return true;
    }

    public void RebuildHierarchyIndex()
    {
        _childrenIndex.Clear();

        foreach (var (entity, parentComp) in Context.View<ParentComponent>())
        {
            if (parentComp.ParentId is not int parentId)
                continue;

            if (!Context.Contains(parentId))
            {
                Logger.Warning(
                    "Orphan entity '{EntityName}' (ID: {EntityId}): ParentId {ParentId} missing — detaching to root",
                    entity.Name, entity.Id, parentId);
                parentComp.ParentId = null;
                continue;
            }

            // Cycle check: walk ancestors; first repeat → detach this entity to root
            var visited = new HashSet<int> { entity.Id };
            var walkId = parentId;
            var cyclic = false;
            while (true)
            {
                if (!visited.Add(walkId))
                {
                    Logger.Warning(
                        "Hierarchy cycle at entity '{EntityName}' (ID: {EntityId}) — detaching to root",
                        entity.Name, entity.Id);
                    parentComp.ParentId = null;
                    cyclic = true;
                    break;
                }

                if (!Context.Contains(walkId))
                    break;

                var ancestor = Context.GetById(walkId);
                if (!ancestor.TryGetComponent<ParentComponent>(out var ancestorParent) ||
                    ancestorParent.ParentId is not int nextId)
                    break;

                walkId = nextId;
            }

            if (cyclic || parentComp.ParentId is not int resolvedParentId)
                continue;

            if (!_childrenIndex.TryGetValue(resolvedParentId, out var list))
            {
                list = [];
                _childrenIndex[resolvedParentId] = list;
            }

            list.Add(entity);
        }
    }

    public void UpdateWorldTransforms()
    {
        foreach (var root in GetRootEntities())
            ComputeWorldTransform(root, Matrix4x4.Identity);
    }

    public Vector3 GetWorldPosition(Entity entity)
    {
        if (!entity.TryGetComponent<TransformComponent>(out var transform))
            return Vector3.Zero;

        return transform.GetWorldTransform().Translation;
    }

    private void ComputeWorldTransform(Entity entity, Matrix4x4 parentWorld)
    {
        if (entity.TryGetComponent<TransformComponent>(out var transform))
        {
            // Row-vector convention: local then parent → local * parentWorld
            var world = transform.GetTransform() * parentWorld;
            transform.SetWorldTransform(world);
            parentWorld = world;
        }

        foreach (var child in GetChildren(entity))
            ComputeWorldTransform(child, parentWorld);
    }

    private void DetachFromParentIndex(Entity child)
    {
        if (!child.TryGetComponent<ParentComponent>(out var parentComp) || parentComp.ParentId is not int oldParentId)
            return;

        if (_childrenIndex.TryGetValue(oldParentId, out var list))
            list.RemoveAll(e => e.Id == child.Id);
    }

    public IReadOnlyList<Entity> CollectSubtree(Entity root)
    {
        var result = new List<Entity>();
        void Visit(Entity e)
        {
            result.Add(e);
            foreach (var child in GetChildren(e))
                Visit(child);
        }

        Visit(root);
        return result;
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
        _childrenIndex.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
        Logger.Debug("Scene '{Path}' disposed successfully", _path);
    }
}
