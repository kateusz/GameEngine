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
    private uint _viewportWidth;
    private uint _viewportHeight;
    private readonly World _physicsWorld;
    private int _nextEntityId = 1;
    private readonly ISystemManager _systemManager;
    private bool _disposed = false;

    // Cache for tileset textures in editor mode (to avoid loading from disk every frame)
    // Key format: "path|columns|rows" to handle different grid configurations of the same texture
    private readonly Dictionary<string, TileSet> _editorTileSetCache = new();

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

    public IEnumerable<Entity> Entities => _context.Entities;

    public Entity CreateEntity(string name)
    {
        var entity = Entity.Create(_nextEntityId++, name);
        entity.OnComponentAdded += OnComponentAdded;
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
        
        entity.OnComponentAdded += OnComponentAdded;

        _context.Register(entity);
    }

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
        entity.OnComponentAdded -= OnComponentAdded;
        _context.Remove(entity.Id);
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

        // Render 2D sprites using the editor viewport camera
        _graphics2D.BeginScene(camera);

        // Sprites
        var spriteGroup = _context.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
        foreach (var entity in spriteGroup)
        {
            var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();
            _graphics2D.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
        }

        // Subtextures (mirror runtime system)
        var subtextureGroup =
            _context.GetGroup([typeof(TransformComponent), typeof(SubTextureRendererComponent)]);
        foreach (var entity in subtextureGroup)
        {
            var subtextureComponent = entity.GetComponent<SubTextureRendererComponent>();
            if (subtextureComponent.Texture is null) continue;

            // Create SubTexture2D using component's cell/sprite sizes (same as runtime)
            var subTexture = SubTexture2D.CreateFromCoords(
                subtextureComponent.Texture,
                subtextureComponent.Coords,
                subtextureComponent.CellSize,
                subtextureComponent.SpriteSize
            );

            // Use transform directly without additional scaling (same as runtime)
            var transform = entity.GetComponent<TransformComponent>().GetTransform();
            _graphics2D.DrawQuad(transform, subTexture.Texture, subTexture.TexCoords, entityId: entity.Id);
        }

        // TileMaps (mirror runtime system with caching)
        var tilemapGroup = _context.GetGroup([typeof(TransformComponent), typeof(TileMapComponent)]);
        foreach (var entity in tilemapGroup)
        {
            var tileMapComponent = entity.GetComponent<TileMapComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();

            // Skip if no tileset path
            if (string.IsNullOrEmpty(tileMapComponent.TileSetPath))
                continue;

            // Get or load cached tileset
            var tileSet = GetOrLoadEditorTileSet(tileMapComponent);
            if (tileSet?.Texture == null)
                continue;

            // Render layers in Z-index order
            var sortedLayers = tileMapComponent.Layers.OrderBy(l => l.ZIndex).ToList();

            foreach (var layer in sortedLayers)
            {
                if (!layer.Visible)
                    continue;

                for (var y = 0; y < tileMapComponent.Height; y++)
                {
                    for (var x = 0; x < tileMapComponent.Width; x++)
                    {
                        var tileId = layer.Tiles[x, y];
                        if (tileId < 0)
                            continue; // Empty tile

                        // Get pre-computed subtexture from cache
                        var subTexture = tileSet.GetTileSubTexture(tileId);
                        if (subTexture == null)
                            continue;

                        // Calculate tile position in world space
                        var tilePos = new Vector3(
                            transformComponent.Translation.X + x * tileMapComponent.TileSize.X,
                            transformComponent.Translation.Y + y * tileMapComponent.TileSize.Y,
                            transformComponent.Translation.Z + layer.ZIndex * 0.01f
                        );

                        // Create transform for this tile
                        var tileTransform = Matrix4x4.CreateScale(new Vector3(tileMapComponent.TileSize, 1.0f)) *
                                            Matrix4x4.CreateRotationZ(transformComponent.Rotation.Z) *
                                            Matrix4x4.CreateTranslation(tilePos);

                        var tintColor = new Vector4(1, 1, 1, 1);

                        _graphics2D.DrawQuad(
                            tileTransform,
                            subTexture.Texture,
                            subTexture.TexCoords,
                            1.0f,
                            tintColor,
                            entity.Id
                        );
                    }
                }
            }
        }

        _graphics2D.EndScene();
    }

    public void OnViewportResize(uint width, uint height)
    {
        _viewportWidth = width;
        _viewportHeight = height;

        var group = _context.GetGroup([typeof(CameraComponent)]);
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
        var view = _context.View<CameraComponent>();
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
            var componentType = component.GetType();

            // Get the generic AddComponent<T>(T component) method
            var addComponentMethod = typeof(Entity)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == nameof(Entity.AddComponent) &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType.IsGenericParameter);

            if (addComponentMethod != null)
            {
                // Make the generic method concrete with the component type
                var genericMethod = addComponentMethod.MakeGenericMethod(componentType);
                genericMethod.Invoke(newEntity, new[] { clonedComponent });
            }
            else
            {
                Logger.Warning("Could not find AddComponent method for type {ComponentTypeName}", componentType.Name);
            }
        }

        return newEntity;
    }

    /// <summary>
    /// Gets or loads a tileset from the editor cache. This prevents loading textures from disk every frame.
    /// </summary>
    private TileSet? GetOrLoadEditorTileSet(TileMapComponent tileMapComponent)
    {
        // Generate cache key that includes grid dimensions
        var cacheKey =
            $"{tileMapComponent.TileSetPath}|{tileMapComponent.TileSetColumns}|{tileMapComponent.TileSetRows}";
        
        // Check cache first
        if (_editorTileSetCache.TryGetValue(cacheKey, out var cachedTileSet))
        {
            return cachedTileSet;
        }

        // Not in cache, load it
        var tileSet = new TileSet
        {
            TexturePath = tileMapComponent.TileSetPath,
            Columns = tileMapComponent.TileSetColumns,
            Rows = tileMapComponent.TileSetRows
        };

        tileSet.LoadTexture(_textureFactory);

        if (tileSet.Texture != null)
        {
            // Calculate tile dimensions from texture size
            tileSet.TileWidth = tileSet.Texture.Width / tileMapComponent.TileSetColumns;
            tileSet.TileHeight = tileSet.Texture.Height / tileMapComponent.TileSetRows;

            // Generate all tile subtextures
            tileSet.GenerateTiles();

            // Cache for future frames
            _editorTileSetCache[cacheKey] = tileSet;
            return tileSet;
        }

        return null;
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
        foreach (var entity in _context.Entities)
        {
            entity.OnComponentAdded -= OnComponentAdded;
        }

        // Dispose SystemManager which will dispose per-scene systems (PhysicsSimulationSystem)
        // Singleton systems (rendering, scripts) are shared and won't be disposed
        _systemManager?.Dispose();

        // Clear tileset cache to prevent memory leaks
        foreach (var tileSet in _editorTileSetCache.Values)
        {
            tileSet.Texture?.Dispose();
        }

        _editorTileSetCache.Clear();

        // Clear entity storage
        _context.Clear();

        _disposed = true;
        Logger.Debug("Scene '{Path}' disposed successfully", _path);
    }
}