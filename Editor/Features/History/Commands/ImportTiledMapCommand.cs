using Engine.Scene;
using Editor.Features.Tiled;

namespace Editor.Features.History.Commands;

public sealed class ImportTiledMapCommand(
    IScene scene,
    TiledMapData data,
    string sourceMapPath,
    string entityName) : IUndoCommand
{
    public int? CreatedId { get; private set; }

    public bool Execute()
    {
        if (CreatedId is { } id && scene.Context.Contains(id))
            scene.DestroyEntity(scene.Context.GetById(id));

        var map = TiledMapApplier.CreateMap(scene, data, sourceMapPath, entityName);
        CreatedId = map.Id;
        return true;
    }

    public void Undo()
    {
        if (CreatedId is { } id && scene.Context.Contains(id))
            scene.DestroyEntity(scene.Context.GetById(id));
        CreatedId = null;
    }
}
