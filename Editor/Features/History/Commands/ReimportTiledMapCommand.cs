using ECS;
using Engine.Scene;
using Editor.Features.Tiled;
using SceneComponents.Rendering;

namespace Editor.Features.History.Commands;

public sealed class ReimportTiledMapCommand(
    IScene scene,
    Entity mapEntity,
    TiledMapData data,
    string sourceMapPath) : IUndoCommand
{
    private TileMapComponent? _oldTilemap;
    private List<EntitySubtreeSnapshot>? _oldChildren;

    public bool Execute()
    {
        if (_oldTilemap is null)
        {
            if (mapEntity.TryGetComponent<TileMapComponent>(out var current))
                _oldTilemap = (TileMapComponent)current.Clone();
            _oldChildren = EntitySubtreeSnapshot.CaptureChildren(scene, mapEntity);
        }

        TiledMapApplier.Reimport(scene, mapEntity, data, sourceMapPath);
        return true;
    }

    public void Undo()
    {
        foreach (var child in scene.GetChildren(mapEntity).ToList())
            scene.DestroyEntity(child);

        if (mapEntity.TryGetComponent<TileMapComponent>(out var tilemap) && _oldTilemap is not null)
            tilemap.CopyFrom(_oldTilemap);

        if (_oldChildren is null)
            return;
        foreach (var snapshot in _oldChildren)
            snapshot.Restore(scene);
    }
}
