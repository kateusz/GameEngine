using ECS;
using Editor.UI.Drawers;
using Engine;
using Engine.Scene.Serializer;
using Serilog;

namespace Editor.UI.Elements;

public class PrefabDropTarget
{
    private static readonly ILogger Logger = Log.ForContext(typeof(PrefabDropTarget));

    private readonly IPrefabSerializer _prefabSerializer;
    private readonly IAssetsManager _assetsManager;

    public PrefabDropTarget(IPrefabSerializer prefabSerializer, IAssetsManager assetsManager)
    {
        _prefabSerializer = prefabSerializer;
        _assetsManager = assetsManager;
    }

    public void HandleEntityDrop(Entity entity)
    {
        var validator = DragDropDrawer.CreateExtensionValidator(
            [".prefab"],
            checkFileExists: false);

        DragDropDrawer.HandleFileDropTarget(
            DragDropDrawer.ContentBrowserItemPayload,
            validator,
            onDropped: path =>
            {
                try
                {
                    var fullPath = Path.Combine(_assetsManager.AssetsPath, path);
                    _prefabSerializer.ApplyPrefabToEntity(entity, fullPath);
                    Logger.Information("Applied prefab {Path} to entity {EntityName}", path, entity.Name);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to apply prefab");
                }
            });
    }
}