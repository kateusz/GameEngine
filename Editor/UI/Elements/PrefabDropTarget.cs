using ECS;
using Editor.UI.Drawers;
using Engine;
using Engine.Scene.Serializer;
using Serilog;

namespace Editor.UI.Elements;

public class PrefabDropTarget(IPrefabSerializer prefabSerializer, IAssetsManager assetsManager)
{
    private static readonly ILogger Logger = Log.ForContext(typeof(PrefabDropTarget));

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
                    var fullPath = Path.Combine(assetsManager.AssetsPath, path);
                    prefabSerializer.ApplyPrefabToEntity(entity, fullPath);
                    Logger.Information("Applied prefab {Path} to entity {EntityName}", path, entity.Name);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to apply prefab");
                }
            });
    }
}