using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering tilemaps
/// </summary>
public class TileMapRenderSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<TileMapRenderSystem>();

    private readonly IGraphics2D _graphics2D;
    private readonly IContext _context;
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly HashSet<int> _loggedEntities = new();

    public int Priority => 190; // Render before sprites

    public TileMapRenderSystem(IGraphics2D graphics2D, IContext context)
    {
        _graphics2D = graphics2D;
        _context = context;
    }

    public void OnInit()
    {
        _loadedTileSets.Clear();
    }

    public void OnShutdown()
    {
        _loadedTileSets.Clear();
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        // Find the primary camera
        Camera? mainCamera = null;
        var cameraGroup = _context.GetGroup([typeof(TransformComponent), typeof(CameraComponent)]);
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

        // Only render if we have a camera
        if (mainCamera == null)
            return;

        // Begin rendering with the camera's view and projection
        _graphics2D.BeginScene(mainCamera, cameraTransform);

        var entities = _context.GetGroup([typeof(TileMapComponent), typeof(TransformComponent)]);

        foreach (var entity in entities)
        {
            var tileMap = entity.GetComponent<TileMapComponent>();
            var transform = entity.GetComponent<TransformComponent>();

            if (string.IsNullOrEmpty(tileMap.TileSetPath))
            {
                Logger.Warning("TileMap entity {EntityId} has empty TileSetPath", entity.Id);
                continue;
            }

            // Load or get cached tileset
            var tileSet = GetOrLoadTileSet(tileMap);
            if (tileSet?.Texture == null)
            {
                Logger.Warning("Failed to load tileset for path: {TileSetPath}", tileMap.TileSetPath);
                continue;
            }

            RenderTileMap(tileMap, tileSet, transform, entity.Id);
        }

        // End the rendering batch
        _graphics2D.EndScene();
    }

    private TileSet? GetOrLoadTileSet(TileMapComponent tileMap)
    {
        if (_loadedTileSets.TryGetValue(tileMap.TileSetPath, out var cachedSet))
        {
            return cachedSet;
        }

        var tileSet = new TileSet
        {
            TexturePath = tileMap.TileSetPath,
            Columns = tileMap.TileSetColumns,
            Rows = tileMap.TileSetRows
        };

        tileSet.LoadTexture();

        if (tileSet.Texture != null)
        {
            // Calculate tile dimensions from texture size
            tileSet.TileWidth = tileSet.Texture.Width / tileMap.TileSetColumns;
            tileSet.TileHeight = tileSet.Texture.Height / tileMap.TileSetRows;

            Logger.Information("Loaded tileset: {Width}x{Height}, TileSize: {TileWidth}x{TileHeight}, Grid: {Columns}x{Rows}",
                tileSet.Texture.Width, tileSet.Texture.Height,
                tileSet.TileWidth, tileSet.TileHeight,
                tileSet.Columns, tileSet.Rows);

            tileSet.GenerateTiles();
            _loadedTileSets[tileMap.TileSetPath] = tileSet;
            return tileSet;
        }

        Logger.Error("Failed to load texture for tileset: {TileSetPath}", tileMap.TileSetPath);
        return null;
    }

    private void RenderTileMap(TileMapComponent tileMap, TileSet tileSet, TransformComponent transform, int entityId)
    {
        // Log tilemap info once per entity
        if (!_loggedEntities.Contains(entityId))
        {
            Logger.Information("=== TileMap Rendering Info for Entity {EntityId} ===", entityId);
            Logger.Information("  Transform Position: ({X}, {Y}, {Z})", transform.Translation.X, transform.Translation.Y, transform.Translation.Z);
            Logger.Information("  Transform Rotation: ({X}, {Y}, {Z})", transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z);
            Logger.Information("  Transform Scale: ({X}, {Y}, {Z})", transform.Scale.X, transform.Scale.Y, transform.Scale.Z);
            Logger.Information("  TileMap Size: {Width}x{Height} tiles", tileMap.Width, tileMap.Height);
            Logger.Information("  Tile Size (world units): {TileWidth}x{TileHeight}", tileMap.TileSize.X, tileMap.TileSize.Y);
            Logger.Information("  Total world size: {WorldWidth}x{WorldHeight} units",
                tileMap.Width * tileMap.TileSize.X, tileMap.Height * tileMap.TileSize.Y);
            Logger.Information("  World bounds: X:[{MinX} to {MaxX}], Y:[{MinY} to {MaxY}]",
                transform.Translation.X,
                transform.Translation.X + tileMap.Width * tileMap.TileSize.X,
                transform.Translation.Y,
                transform.Translation.Y + tileMap.Height * tileMap.TileSize.Y);
            Logger.Information("  Layers: {LayerCount}", tileMap.Layers.Count);

            // Log first few tile positions
            Logger.Information("  Sample tile positions:");
            var sampleCount = 0;
            foreach (var layer in tileMap.Layers.Take(1))
            {
                var maxY = tileMap.Height < 3 ? tileMap.Height : 3;
                var maxX = tileMap.Width < 3 ? tileMap.Width : 3;

                for (var y = 0; y < maxY; y++)
                {
                    for (var x = 0; x < maxX; x++)
                    {
                        var tileId = layer.Tiles[x, y];
                        if (tileId >= 0)
                        {
                            var posX = transform.Translation.X + x * tileMap.TileSize.X;
                            var posY = transform.Translation.Y + y * tileMap.TileSize.Y;
                            Logger.Information("    Tile[{X},{Y}] ID:{TileId} at world pos ({PosX}, {PosY})",
                                x, y, tileId, posX, posY);
                            sampleCount++;
                            if (sampleCount >= 5) break;
                        }
                    }
                    if (sampleCount >= 5) break;
                }
            }

            _loggedEntities.Add(entityId);
        }

        // Sort layers by Z-index
        var sortedLayers = tileMap.Layers.OrderBy(l => l.ZIndex).ToList();

        foreach (var layer in sortedLayers)
        {
            if (!layer.Visible)
            {
                continue;
            }

            for (var y = 0; y < tileMap.Height; y++)
            {
                for (var x = 0; x < tileMap.Width; x++)
                {
                    var tileId = layer.Tiles[x, y];
                    if (tileId < 0)
                        continue; // Empty tile

                    var subTexture = tileSet.GetTileSubTexture(tileId);
                    if (subTexture == null)
                    {
                        Logger.Warning("Failed to get subtexture for tile ID {TileId} at ({X}, {Y})", tileId, x, y);
                        continue;
                    }

                    // Calculate tile position in world space
                    var tilePos = new Vector3(
                        transform.Translation.X + x * tileMap.TileSize.X,
                        transform.Translation.Y + y * tileMap.TileSize.Y,
                        transform.Translation.Z + layer.ZIndex * 0.01f
                    );

                    // Create transform for this tile
                    // Each tile has its own size (TileSize), not affected by entity scale
                    var tileTransform = Matrix4x4.CreateScale(new Vector3(tileMap.TileSize, 1.0f)) *
                                      Matrix4x4.CreateRotationZ(transform.Rotation.Z) *
                                      Matrix4x4.CreateTranslation(tilePos);

                    var tintColor = new Vector4(1, 1, 1, layer.Opacity);

                    _graphics2D.DrawQuad(
                        tileTransform,
                        subTexture.Texture,
                        subTexture.TexCoords,
                        1.0f,
                        tintColor,
                        entityId
                    );
                }
            }
        }
    }
}


