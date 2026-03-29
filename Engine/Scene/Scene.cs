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

        return entity;
    }

    public void AddEntity(Entity entity)
    {
        if (entity.Id <= 0)
            throw new ArgumentException($"Entity ID must be positive, got {entity.Id}", nameof(entity));

        // Track the highest ID when adding existing entities (e.g., from deserialization)
        if (entity.Id >= _nextEntityId)
            _nextEntityId = entity.Id + 1;

        context.Register(entity);
        _entities.Add(entity);

        // Normalize primary camera flags to ensure at most one primary camera
        if (entity.HasComponent<CameraComponent>() && entity.GetComponent<CameraComponent>().Primary)
            SetPrimaryCamera(entity);
    }

    public void DestroyEntity(Entity entity)
    {
        context.Remove(entity.Id);
        _entities.Remove(entity);
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

        var lightGroup = context.View<LightingComponent>().ToList();
        if (lightGroup.Count > 0)
        {
            var (_, firstLight) = lightGroup[0];
            graphics3D.SetLightPosition(firstLight.Position);
            graphics3D.SetLightDirection(firstLight.Direction);
            graphics3D.SetLightType((int)firstLight.Type);
            graphics3D.SetLightColor(firstLight.Color);

            graphics3D.BeginLightVisualization(camera);
            foreach (var (_, lightingComponent) in lightGroup)
                graphics3D.DrawLightVisualization(lightingComponent.Position);
            graphics3D.EndLightVisualization();
        }

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

        Logger.Debug("Disposing scene '{Path}'", path);

        // Dispose SystemManager which will dispose per-scene systems (PhysicsSimulationSystem)
        // Singleton systems (rendering, scripts) are shared and won't be disposed
        _init.SystemManager?.Dispose();

        // Clear entity storage
        context.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
        Logger.Debug("Scene '{Path}' disposed successfully", path);
    }
}
