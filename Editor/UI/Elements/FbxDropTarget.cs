using Editor.UI.Drawers;
using Engine.Core;
using Engine.Scene;
using Serilog;

namespace Editor.UI.Elements;

public class FbxDropTarget(
    ModelSceneImporter modelSceneImporter,
    ISceneContext sceneContext,
    IAssetsManager assetsManager)
{
    private static readonly ILogger Logger = Log.ForContext<FbxDropTarget>();

    private static readonly string[] SupportedExtensions = [".obj", ".fbx", ".gltf", ".glb"];

    public ModelSceneImporter.ImportResult? HandleViewportDrop()
    {
        ModelSceneImporter.ImportResult? importResult = null;

        DragDropDrawer.HandleFileDropTarget(
            DragDropDrawer.ContentBrowserItemPayload,
            path => DragDropDrawer.HasValidExtension(path, SupportedExtensions),
            path =>
            {
                try
                {
                    var fullPath = Path.Combine(assetsManager.AssetsPath, path);
                    var scene = sceneContext.ActiveScene;

                    importResult = modelSceneImporter.Import(
                        scene, fullPath,
                        addDefaultLighting: true,
                        addCamera: false);

                    Logger.Information(
                        "Imported model {Path}: {Count} mesh entities, radius {Radius:F1}",
                        path, importResult.MeshEntities.Count, importResult.SceneRadius);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to import model {Path}", path);
                }
            });

        return importResult;
    }
}
