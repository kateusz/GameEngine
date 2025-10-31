using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Scene.Components;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering tilemaps
/// </summary>
public class TileMapRenderSystem : ISystem
{
    private readonly IGraphics2D _graphics2D;
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();

    public int Priority => 190; // Render before sprites

    public TileMapRenderSystem(IGraphics2D graphics2D)
    {
        _graphics2D = graphics2D;
    }

    public void OnInit() { }

    public void OnShutdown()
    {
        _loadedTileSets.Clear();
    }

    public void OnUpdate(TimeSpan deltaTime) { }

    public void OnUpdate(Context context, float deltaTime)
    {
        var entities = Context.Instance.GetGroup([typeof(TileMapComponent), typeof(TransformComponent)]);
        
        foreach (var entity in entities)
        {
            var tileMap = entity.GetComponent<TileMapComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            
            if (string.IsNullOrEmpty(tileMap.TileSetPath))
                continue;

            // Load or get cached tileset
            var tileSet = GetOrLoadTileSet(tileMap);
            if (tileSet?.Texture == null)
                continue;

            RenderTileMap(tileMap, tileSet, transform, entity.Id);
        }
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
            
            tileSet.GenerateTiles();
            _loadedTileSets[tileMap.TileSetPath] = tileSet;
            return tileSet;
        }

        return null;
    }

    private void RenderTileMap(TileMapComponent tileMap, TileSet tileSet, TransformComponent transform, int entityId)
    {
        // Sort layers by Z-index
        var sortedLayers = tileMap.Layers.OrderBy(l => l.ZIndex).ToList();

        foreach (var layer in sortedLayers)
        {
            if (!layer.Visible)
                continue;

            for (int y = 0; y < tileMap.Height; y++)
            {
                for (int x = 0; x < tileMap.Width; x++)
                {
                    int tileId = layer.Tiles[x, y];
                    if (tileId < 0)
                        continue; // Empty tile

                    var subTexture = tileSet.GetTileSubTexture(tileId);
                    if (subTexture == null)
                        continue;
                    
                    // Calculate tile position in world space
                    var tilePos = new Vector3(
                        transform.Translation.X + x * tileMap.TileSize.X,
                        transform.Translation.Y + y * tileMap.TileSize.Y,
                        transform.Translation.Z + layer.ZIndex * 0.01f
                    );

                    // Create transform for this tile
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


