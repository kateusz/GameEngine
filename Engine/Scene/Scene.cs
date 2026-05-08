using System.Numerics;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Lights;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene;

internal sealed class Scene : IScene
{
    private static readonly ILogger Logger = Log.ForContext<Scene>();
    private static readonly Vector2[] DefaultTextureCoords =
    [
        new(0.0f, 0.0f),
        new(1.0f, 0.0f),
        new(1.0f, 1.0f),
        new(0.0f, 1.0f)
    ];

    private readonly (ISystemManager SystemManager, World PhysicsWorld) _init;
    private int _nextEntityId = 1;
    private bool _disposed;
    private readonly List<Entity> _entities = [];
    private readonly string _path;
    private readonly string _sceneName;
    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;
    private readonly ITextureFactory? _textureFactory;
    private readonly IContext _context;
    private readonly DebugSettings _debugSettings;
    private readonly ISystemManager _systemManager;

    public Scene(string path,
        string sceneName,
        ISceneSystemRegistry systemRegistry,
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        ITextureFactory textureFactory,
        IContext context,
        DebugSettings debugSettings,
        ISystemManager systemManager)
    {
        _path = path;
        _sceneName = sceneName;
        _graphics2D = graphics2D;
        _graphics3D = graphics3D;
        _textureFactory = textureFactory;
        _context = context;
        _debugSettings = debugSettings;
        _systemManager = systemManager;
        _init = Initialize(systemRegistry, context);
    }

    public Scene(string path,
        string sceneName,
        ISceneSystemRegistry systemRegistry,
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        IContext context,
        DebugSettings debugSettings,
        ISystemManager systemManager)
        : this(path, sceneName, systemRegistry, graphics2D, graphics3D, textureFactory: null!, context, debugSettings, systemManager)
    {
    }
    
    private (ISystemManager, World) Initialize(ISceneSystemRegistry systemRegistry, IContext context)
    {
        // Populate system manager from registry (singleton systems shared across scenes)
        systemRegistry.PopulateSystemManager(_systemManager);

        var physicsWorld = new World(new Vector2(0, -9.8f));
        var contactListener = new SceneContactListener();
        physicsWorld.SetContactListener(contactListener);

        // Create and register physics simulation system with the physics world
        // NOTE: This system is per-scene because each scene has its own physics world
        var physicsSimulationSystem = new PhysicsSimulationSystem(physicsWorld, context);
        _systemManager.RegisterSystem(physicsSimulationSystem);

        return (_systemManager, physicsWorld);
    }

    public string Name => _sceneName;
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

        // Normalize primary camera flags to ensure at most one primary camera
        if (entity.HasComponent<CameraComponent>() && entity.GetComponent<CameraComponent>().Primary)
            SetPrimaryCamera(entity);
    }

    public void DestroyEntity(Entity entity)
    {
        _context.Remove(entity.Id);
        _entities.Remove(entity);
    }

    public void OnRuntimeStart()
    {
        _init.SystemManager.Initialize();
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
        var pointLights = _context.View<PointLightComponent>().ToList();
        var directionalLights = _context.View<DirectionalLightComponent>().ToList();
        var ambientLights = _context.View<AmbientLightComponent>().ToList();

        if (directionalLights.Count > 0)
        {
            var (_, directionalLight) = directionalLights[0];
            _graphics3D.SetDirectionalLight(
                enabled: true,
                direction: directionalLight.Direction,
                color: directionalLight.Color,
                strength: directionalLight.Strength);
        }
        else
        {
            _graphics3D.SetDirectionalLight(
                enabled: false,
                direction: default,
                color: default,
                strength: 0.0f);
        }

        if (ambientLights.Count > 0)
        {
            var (_, ambientLight) = ambientLights[0];
            _graphics3D.SetAmbientLight(
                enabled: true,
                color: ambientLight.Color,
                strength: ambientLight.Strength);
        }
        else
        {
            _graphics3D.SetAmbientLight(
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
        _graphics3D.SetPointLights(pointLightData);

        _graphics3D.BeginScene(camera);
        
        var modelGroup = _context.View<ModelRendererComponent>();
        
        foreach (var (entity, modelRendererComponent) in modelGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var meshComponent = entity.GetComponent<MeshComponent>();
        
            _graphics3D.DrawModel(transformComponent.GetTransform(), meshComponent, modelRendererComponent,
                entity.Id);
        }
        
        _graphics3D.EndScene();
        
        _graphics3D.BeginLightVisualization(camera);
        foreach (var (e, _) in pointLights)
        {
            if (!e.TryGetComponent<TransformComponent>(out var transform))
                continue;

            _graphics3D.DrawLightVisualization(transform.Translation);
        }

        foreach (var (e, _) in directionalLights)
        {
            if (!e.TryGetComponent<TransformComponent>(out var transform))
                continue;

            _graphics3D.DrawLightVisualization(transform.Translation);
        }
        _graphics3D.EndLightVisualization();

        _graphics2D.BeginScene(camera);

        var spriteGroup = _context.View<SpriteRendererComponent>();
        foreach (var (entity, spriteRendererComponent) in spriteGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var texture = ResolveTexture(spriteRendererComponent.TexturePath);
            if (texture is not null)
                _graphics2D.DrawQuad(transformComponent.GetTransform(), texture, DefaultTextureCoords, spriteRendererComponent.TilingFactor, spriteRendererComponent.Color, entity.Id);
            else
                _graphics2D.DrawQuad(transformComponent.GetTransform(), spriteRendererComponent.Color, entity.Id);
        }

        var subtextureGroup = _context.View<SubTextureRendererComponent>();
        foreach (var (entity, subtextureComponent) in subtextureGroup)
        {
            var texture = ResolveTexture(subtextureComponent.TexturePath);
            if (texture is null)
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
                    texture,
                    subtextureComponent.Coords,
                    subtextureComponent.CellSize,
                    subtextureComponent.SpriteSize
                );
                texCoords = subTexture.TexCoords;
            }

            // Use transform directly without additional scaling (same as runtime)
            var transform = entity.GetComponent<TransformComponent>().GetTransform();
            _graphics2D.DrawQuad(transform, texture, texCoords, entityId: entity.Id);
        }

        if (_debugSettings.ShowColliderBounds)
        {
            foreach (var (entity, boxCollider) in _context.View<BoxCollider2DComponent>())
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
                _graphics2D.DrawRect(trs, color, entity.Id);
            }
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
        if (!_entities.Contains(cameraEntity))
            throw new ArgumentException("Entity does not belong to this scene", nameof(cameraEntity));

        if (!cameraEntity.HasComponent<CameraComponent>())
            throw new ArgumentException("Entity must have a CameraComponent", nameof(cameraEntity));

        var view = _context.View<CameraComponent>();
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

    private Texture2D? ResolveTexture(string? texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
            return null;

        return _textureFactory?.Create(PathBuilder.Build(texturePath));
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

        // Dispose SystemManager which will dispose per-scene systems (PhysicsSimulationSystem)
        // Singleton systems (rendering, scripts) are shared and won't be disposed
        _init.SystemManager?.Dispose();

        // Clear entity storage
        _context.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
        Logger.Debug("Scene '{Path}' disposed successfully", _path);
    }
}
