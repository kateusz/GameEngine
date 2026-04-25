using System.Numerics;
using Box2D.NetStandard.Collision.Shapes;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Engine.Scene.Components.Lights;
using Engine.Scene.Systems;
using Serilog;

namespace Engine.Scene;

internal sealed class Scene(
    string path,
    string sceneName,
    ISceneSystemRegistry systemRegistry,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IContext context,
    DebugSettings debugSettings) : IScene
{
    private static readonly ILogger Logger = Log.ForContext<Scene>();

    private readonly (ISystemManager SystemManager, World PhysicsWorld) _init = Initialize(systemRegistry, context);
    private int _nextEntityId = 1;
    private bool _disposed;
    private readonly List<Entity> _entities = [];
    private readonly Dictionary<int, Action> _hookedEntityHandlers = new();
    private static (ISystemManager, World) Initialize(ISceneSystemRegistry systemRegistry, IContext context)
    {
        var systemManager = new SystemManager();

        // Populate system manager from registry (singleton systems shared across scenes)
        systemRegistry.PopulateSystemManager(systemManager);

        var physicsWorld = new World(new Vector2(0, -9.8f));
        var contactListener = new SceneContactListener();
        physicsWorld.SetContactListener(contactListener);

        // Create and register physics simulation system with the physics world
        // NOTE: This system is per-scene because each scene has its own physics world
        var physicsSimulationSystem = new PhysicsSimulationSystem(physicsWorld, context);
        systemManager.RegisterSystem(physicsSimulationSystem);

        return (systemManager, physicsWorld);
    }

    public string Name => sceneName;
    public IEnumerable<Entity> Entities => _entities;

    public Entity CreateEntity(string name)
    {
        var entity = Entity.Create(_nextEntityId++, name);
        context.Register(entity);
        _entities.Add(entity);
        HookHierarchy(entity);
        return entity;
    }

    public void AddEntity(Entity entity)
    {
        if (entity.Id <= 0)
            throw new ArgumentException($"Entity ID must be positive, got {entity.Id}", nameof(entity));

        if (entity.Id >= _nextEntityId)
            _nextEntityId = entity.Id + 1;

        context.Register(entity);
        _entities.Add(entity);
        HookHierarchy(entity);

        if (entity.HasComponent<CameraComponent>() && entity.GetComponent<CameraComponent>().Primary)
            SetPrimaryCamera(entity);
    }

    public void DestroyEntity(Entity entity)
    {
        // Detach from parent's child list (only the destroyed root entry — the descendants
        // are removed wholesale below).
        if (entity.TryGetComponent<TransformComponent>(out var t) && t.ParentId is { } parentId)
        {
            var parent = _entities.FirstOrDefault(e => e.Id == parentId);
            if (parent is not null && parent.TryGetComponent<TransformComponent>(out var parentT))
                parentT.RemoveChildIdInternal(entity.Id);
        }

        // Collect descendants in post-order (deepest first).
        var toDestroy = new List<Entity>();
        CollectSubtreePostOrder(entity, toDestroy);

        foreach (var e in toDestroy)
        {
            UnhookHierarchy(e);
            context.Remove(e.Id);
            _entities.Remove(e);
        }
    }

    private void CollectSubtreePostOrder(Entity entity, List<Entity> output)
    {
        if (entity.TryGetComponent<TransformComponent>(out var t))
        {
            // Copy the child list because the recursion path can mutate it indirectly
            // if a child without TC ever appeared (defensive).
            foreach (var childId in t.ChildIds.ToList())
            {
                var child = _entities.FirstOrDefault(e => e.Id == childId);
                if (child is not null)
                    CollectSubtreePostOrder(child, output);
            }
        }
        output.Add(entity);
    }

    private void HookHierarchy(Entity entity)
    {
        if (_hookedEntityHandlers.ContainsKey(entity.Id))
            return;
        if (!entity.TryGetComponent<TransformComponent>(out var t))
            return;

        Action handler = () => MarkChildrenWorldDirty(entity);
        t.LocalChanged += handler;
        _hookedEntityHandlers[entity.Id] = handler;
    }

    private void UnhookHierarchy(Entity entity)
    {
        if (!_hookedEntityHandlers.Remove(entity.Id, out var handler))
            return;
        if (entity.TryGetComponent<TransformComponent>(out var t))
            t.LocalChanged -= handler;
    }

    private void MarkChildrenWorldDirty(Entity entity)
    {
        if (!entity.TryGetComponent<TransformComponent>(out var t))
            return;
        foreach (var childId in t.ChildIds)
        {
            var child = _entities.FirstOrDefault(e => e.Id == childId);
            if (child is null) continue;
            if (!child.TryGetComponent<TransformComponent>(out var childT)) continue;
            childT.MarkWorldDirty();
            MarkChildrenWorldDirty(child);
        }
    }

    public void OnRuntimeStart()
    {
        _init.SystemManager.Initialize();

        var view = context.View<RigidBody2DComponent>();
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

            var body = _init.PhysicsWorld.CreateBody(bodyDef);
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
                shape.SetAsBox(actualSizeX, actualSizeY, center, 0.0f);

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
        _init.SystemManager.Shutdown();
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
        _init.SystemManager.Update(ts);
    }


    public void OnUpdateEditor(TimeSpan ts, EditorCamera camera)
    {
        var pointLights = context.View<PointLightComponent>().ToList();
        var directionalLights = context.View<DirectionalLightComponent>().ToList();
        var ambientLights = context.View<AmbientLightComponent>().ToList();

        if (directionalLights.Count > 0)
        {
            var (_, directionalLight) = directionalLights[0];
            graphics3D.SetDirectionalLight(
                enabled: true,
                direction: directionalLight.Direction,
                color: directionalLight.Color,
                strength: directionalLight.Strength);
        }
        else
        {
            graphics3D.SetDirectionalLight(
                enabled: false,
                direction: default,
                color: default,
                strength: 0.0f);
        }

        if (ambientLights.Count > 0)
        {
            var (_, ambientLight) = ambientLights[0];
            graphics3D.SetAmbientLight(
                enabled: true,
                color: ambientLight.Color,
                strength: ambientLight.Strength);
        }
        else
        {
            graphics3D.SetAmbientLight(
                enabled: false,
                color: default,
                strength: 0.0f);
        }

        var pointLightData = new List<PointLightData>(16);
        foreach (var (entity, pointLight) in pointLights)
        {
            if (pointLightData.Count >= 16)
                break;
            if (!entity.TryGetComponent<TransformComponent>(out var lightTransform))
                continue;

            pointLightData.Add(new PointLightData(
                lightTransform.Translation,
                pointLight.Color,
                pointLight.Intensity));
        }
        graphics3D.SetPointLights(pointLightData);

        graphics3D.BeginScene(camera);
        
        var modelGroup = context.View<ModelRendererComponent>();
        
        foreach (var (entity, modelRendererComponent) in modelGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var meshComponent = entity.GetComponent<MeshComponent>();
        
            graphics3D.DrawModel(transformComponent.GetTransform(), meshComponent, modelRendererComponent,
                entity.Id);
        }
        
        graphics3D.EndScene();
        
        graphics3D.BeginLightVisualization(camera);
        foreach (var (e, _) in pointLights)
        {
            if (!e.TryGetComponent<TransformComponent>(out var transform))
                continue;

            graphics3D.DrawLightVisualization(transform.Translation);
        }

        foreach (var (e, _) in directionalLights)
        {
            if (!e.TryGetComponent<TransformComponent>(out var transform))
                continue;

            graphics3D.DrawLightVisualization(transform.Translation);
        }
        graphics3D.EndLightVisualization();

        graphics2D.BeginScene(camera);

        var spriteGroup = context.View<SpriteRendererComponent>();
        foreach (var (entity, spriteRendererComponent) in spriteGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            graphics2D.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
        }

        var subtextureGroup = context.View<SubTextureRendererComponent>();
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
            graphics2D.DrawQuad(transform, subtextureComponent.Texture, texCoords, entityId: entity.Id);
        }

        if (debugSettings.ShowColliderBounds)
        {
            foreach (var (entity, boxCollider) in context.View<BoxCollider2DComponent>())
            {
                var transform = entity.GetComponent<TransformComponent>();
                var size = new Vector2(
                    boxCollider.Size.X * 2.0f * transform.Scale.X,
                    boxCollider.Size.Y * 2.0f * transform.Scale.Y
                );
                var color = GetEditorColliderColor(entity);
                var rotation = transform.Rotation.Z;
                var cos = MathF.Cos(rotation);
                var sin = MathF.Sin(rotation);
                var scaledOffset = new Vector2(
                    boxCollider.Offset.X * transform.Scale.X,
                    boxCollider.Offset.Y * transform.Scale.Y
                );
                var rotatedOffset = new Vector2(
                    scaledOffset.X * cos - scaledOffset.Y * sin,
                    scaledOffset.X * sin + scaledOffset.Y * cos
                );
                var worldPos = new Vector3(
                    transform.Translation.X + rotatedOffset.X,
                    transform.Translation.Y + rotatedOffset.Y,
                    0.0f
                );

                var trs = Matrix4x4.CreateTranslation(worldPos)
                          * Matrix4x4.CreateRotationZ(rotation)
                          * Matrix4x4.CreateScale(size.X, size.Y, 1.0f);
                graphics2D.DrawRect(trs, color, entity.Id);
            }
        }

        graphics2D.EndScene();
    }

    public void OnViewportResize(uint width, uint height)
    {
        Logger.Information("Scene.OnViewportResize called: {Width}x{Height}", width, height);

        var group = context.View<CameraComponent>();
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
        var view = context.View<CameraComponent>();
        foreach (var (entity, component) in view)
        {
            if (component.Primary)
                return entity;
        }

        return null;
    }

    public void SetPrimaryCamera(Entity cameraEntity)
    {
        if (!_entities.Contains(cameraEntity))
            throw new ArgumentException("Entity does not belong to this scene", nameof(cameraEntity));

        if (!cameraEntity.HasComponent<CameraComponent>())
            throw new ArgumentException("Entity must have a CameraComponent", nameof(cameraEntity));

        var view = context.View<CameraComponent>();
        foreach (var (entity, component) in view)
        {
            component.Primary = entity.Id == cameraEntity.Id;
        }
    }

    private static Vector4 GetEditorColliderColor(Entity entity)
    {
        if (!entity.TryGetComponent<RigidBody2DComponent>(out var rb))
            return new Vector4(0.0f, 1.0f, 1.0f, 1.0f); // Cyan (no rigid body)

        return rb.BodyType switch
        {
            RigidBodyType.Static => new Vector4(0.0f, 1.0f, 0.0f, 1.0f),    // Bright green
            RigidBodyType.Kinematic => new Vector4(1.0f, 0.5f, 0.0f, 1.0f), // Orange
            _ => new Vector4(1.0f, 0.0f, 0.3f, 1.0f)                        // Magenta
        };
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

    public IEnumerable<Entity> GetRootEntities()
    {
        foreach (var e in _entities)
        {
            if (!e.TryGetComponent<TransformComponent>(out var t) || t.ParentId is null)
                yield return e;
        }
    }

    public IEnumerable<Entity> GetChildren(Entity entity)
    {
        if (!entity.TryGetComponent<TransformComponent>(out var t))
            yield break;

        foreach (var childId in t.ChildIds)
        {
            var child = _entities.FirstOrDefault(e => e.Id == childId);
            if (child is not null)
                yield return child;
        }
    }

    public void SetParent(Entity child, Entity? parent)
    {
        HookHierarchy(child);
        if (parent is not null)
            HookHierarchy(parent);

        if (!child.TryGetComponent<TransformComponent>(out var childT))
            throw new InvalidOperationException(
                $"Entity {child.Id} ('{child.Name}') has no TransformComponent and cannot be parented.");

        if (parent is not null)
        {
            if (parent.Id == child.Id)
                throw new InvalidOperationException("Cannot parent an entity to itself.");

            if (!parent.HasComponent<TransformComponent>())
                throw new InvalidOperationException(
                    $"Parent entity {parent.Id} ('{parent.Name}') has no TransformComponent.");

            var cursorId = parent.Id;
            while (true)
            {
                if (cursorId == child.Id)
                    throw new InvalidOperationException(
                        "Cannot parent entity to its own descendant (would create a cycle).");

                var cursorEntity = _entities.FirstOrDefault(e => e.Id == cursorId);
                if (cursorEntity is null) break;
                if (!cursorEntity.TryGetComponent<TransformComponent>(out var cursorT)) break;
                if (cursorT.ParentId is null) break;
                cursorId = cursorT.ParentId.Value;
            }
        }

        if (childT.ParentId is { } oldParentId)
        {
            var oldParent = _entities.FirstOrDefault(e => e.Id == oldParentId);
            if (oldParent is not null && oldParent.TryGetComponent<TransformComponent>(out var oldParentT))
                oldParentT.RemoveChildIdInternal(child.Id);
        }

        childT.SetParentIdInternal(parent?.Id);

        if (parent is not null)
        {
            var parentT = parent.GetComponent<TransformComponent>();
            parentT.AddChildIdInternal(child.Id);
        }

        MarkSubtreeWorldDirty(child);
    }

    private void MarkSubtreeWorldDirty(Entity root)
    {
        if (!root.TryGetComponent<TransformComponent>(out var rootT))
            return;

        rootT.MarkWorldDirty();
        foreach (var child in GetChildren(root))
            MarkSubtreeWorldDirty(child);
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
    /// Unsubscribes transform hierarchy <see cref="TransformComponent.LocalChanged" /> handlers,
    /// disposes the SystemManager (which handles per-scene systems), and clears entity storage to prevent memory leaks.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        Logger.Debug("Disposing scene '{Path}'", path);

        // Dispose SystemManager which will dispose per-scene systems (PhysicsSimulationSystem)
        // Singleton systems (rendering, scripts) are shared and won't be disposed
        _init.SystemManager?.Dispose();

        foreach (var e in _entities.ToList())
            UnhookHierarchy(e);
        // Clear entity storage
        context.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
        Logger.Debug("Scene '{Path}' disposed successfully", path);
    }
}
