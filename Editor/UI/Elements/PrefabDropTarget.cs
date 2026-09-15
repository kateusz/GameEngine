using ECS;
using Editor.UI.Drawers;
using Engine.Project;
using Engine.Scene;
using Engine.Scene.Serializer;
using Serilog;

namespace Editor.UI.Elements;

public class PrefabDropTarget(IPrefabSerializer prefabSerializer, ISceneContext sceneContext)
{
    private static readonly ILogger Logger = Log.ForContext(typeof(PrefabDropTarget));

    public void HandleEntityDrop(Entity entity)
    {
        DragDropDrawer.HandleFileDropTarget(
            DragDropDrawer.ContentBrowserItemPayload,
            path => DragDropDrawer.HasValidExtension(path, ".prefab"),
            onDropped: path =>
            {
                try
                {
                    var scene = sceneContext.ActiveScene
                                ?? throw new InvalidOperationException("No active scene");
                    var fullPath = PathBuilder.Resolve(path);
                    prefabSerializer.ApplyPrefabToEntity(scene, entity, fullPath);
                    Logger.Information("Applied prefab {Path} to entity {EntityName}", path, entity.Name);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to apply prefab");
                }
            });
    }
}
