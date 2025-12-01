using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Engine.Tiles;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering tilemaps
/// </summary>
internal sealed class TileMapRenderSystem(IGraphics2D graphics2D, IContext context, ITextureFactory textureFactory)
    : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<TileMapRenderSystem>();

    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly HashSet<int> _loggedEntities = [];

    public int Priority => SystemPriorities.TileMapRenderSystem;

    public void OnInit() => _loadedTileSets.Clear();

    public void OnShutdown() => _loadedTileSets.Clear();

    public void OnUpdate(TimeSpan deltaTime)
    {
        var (primaryCamera, cameraTransform) = GetPrimaryCameraAndTransform();
        if (primaryCamera == null || cameraTransform == null)
            return;

        // Begin rendering with the camera's view and projection
        graphics2D.BeginScene(primaryCamera, cameraTransform.Value);

        var entities = context.GetGroup([typeof(TileMapComponent), typeof(TransformComponent)]);

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
        graphics2D.EndScene();
    }

    private (Camera?, Matrix4x4?) GetPrimaryCameraAndTransform()
    {
        var cameraGroup = context.GetGroup([typeof(TransformComponent), typeof(CameraComponent)]);
        foreach (var entity in cameraGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var cameraComponent = entity.GetComponent<CameraComponent>();

            if (cameraComponent.Primary)
            {
                return (cameraComponent.Camera, transformComponent.GetTransform());
            }
        }

        return (null, null);
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

        tileSet.LoadTexture(textureFactory);

        if (tileSet.Texture != null)
        {
            // Calculate tile dimensions from texture size
            tileSet.TileWidth = tileSet.Texture.Width / tileMap.TileSetColumns;
            tileSet.TileHeight = tileSet.Texture.Height / tileMap.TileSetRows;

            Logger.Information(
                "Loaded tileset: {Width}x{Height}, TileSize: {TileWidth}x{TileHeight}, Grid: {Columns}x{Rows}",
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
        if (!_loggedEntities.Contains(entityId))
        {
            LogTileSetInformation(tileMap, transform, entityId);
            _loggedEntities.Add(entityId);
        }

        // Sort layers by Z-index
        var sortedLayers = tileMap.Layers.OrderBy(l => l.ZIndex).ToList();

        foreach (var layer in sortedLayers)
        {
            if (!layer.Visible)
                continue;

            for (var y = 0; y < tileMap.Height; y++)
            {
                for (var x = 0; x < tileMap.Width; x++)
                {
                    var tileId = layer.Tiles[x, y];
                    if (tileId < 0)
                        continue; // Empty tile

                    RenderTile(tileMap, tileSet, transform, entityId, tileId, x, y, layer);
                }
            }
        }
    }

    private void RenderTile(TileMapComponent tileMap, TileSet tileSet, TransformComponent transform, int entityId,
        int tileId, int x, int y, TileMapLayer layer)
    {
        var subTexture = tileSet.GetTileSubTexture(tileId);
        if (subTexture == null)
        {
            Logger.Warning("Failed to get subtexture for tile ID {TileId} at ({X}, {Y})", tileId, x, y);
            return;
        }

        // Calculate tile position in world space
        // Flip Y: y=0 should render at the top, so use (Height-1-y) to invert
        var tilePos = new Vector3(
            transform.Translation.X + x * tileMap.TileSize.X,
            transform.Translation.Y + (tileMap.Height - 1 - y) * tileMap.TileSize.Y,
            transform.Translation.Z + layer.ZIndex * 0.01f
        );

        // Create transform for this tile
        // Each tile has its own size (TileSize), not affected by entity scale
        var tileTransform = Matrix4x4.CreateScale(new Vector3(tileMap.TileSize, 1.0f)) *
                            Matrix4x4.CreateRotationZ(transform.Rotation.Z) *
                            Matrix4x4.CreateTranslation(tilePos);

        var tintColor = new Vector4(1, 1, 1, 1);

        graphics2D.DrawQuad(
            tileTransform,
            subTexture.Texture,
            subTexture.TexCoords,
            1.0f,
            tintColor,
            entityId
        );
    }

    private static void LogTileSetInformation(TileMapComponent tileMap, TransformComponent transform, int entityId)
    {
        Logger.Information("=== TileMap Rendering Info for Entity {EntityId} ===", entityId);
        Logger.Information("  Transform Position: ({X}, {Y}, {Z})", transform.Translation.X, transform.Translation.Y,
            transform.Translation.Z);
        Logger.Information("  Transform Rotation: ({X}, {Y}, {Z})", transform.Rotation.X, transform.Rotation.Y,
            transform.Rotation.Z);
        Logger.Information("  Transform Scale: ({X}, {Y}, {Z})", transform.Scale.X, transform.Scale.Y,
            transform.Scale.Z);
        Logger.Information("  TileMap Size: {Width}x{Height} tiles", tileMap.Width, tileMap.Height);
        Logger.Information("  Tile Size (world units): {TileWidth}x{TileHeight}", tileMap.TileSize.X,
            tileMap.TileSize.Y);
        Logger.Information("  Total world size: {WorldWidth}x{WorldHeight} units",
            tileMap.Width * tileMap.TileSize.X, tileMap.Height * tileMap.TileSize.Y);
        Logger.Information("  World bounds: X:[{MinX} to {MaxX}], Y:[{MinY} to {MaxY}]",
            transform.Translation.X,
            transform.Translation.X + tileMap.Width * tileMap.TileSize.X,
            transform.Translation.Y,
            transform.Translation.Y + tileMap.Height * tileMap.TileSize.Y);
        Logger.Information("  Coordinate system: X increases right, Y increases up, tile[0,0] at bottom-left");
        Logger.Information("  Layers: {LayerCount}", tileMap.Layers.Count);
    }
}