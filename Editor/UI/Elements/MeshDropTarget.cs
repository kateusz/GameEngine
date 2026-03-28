using Editor.UI.Drawers;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene.Components;

namespace Editor.UI.Elements;

/// <summary>
/// UI element that provides drag-and-drop functionality for mesh files.
/// Allows users to drag 3d models from the content browser onto mesh properties.
/// </summary>
public static class MeshDropTarget
{
    private static readonly string[] SupportedExtensions = [".obj"];

    /// <summary>
    /// Draws a drag-and-drop target for mesh files.
    /// </summary>
    /// <param name="meshComponent">Mesh component to update when a mesh is dropped</param>
    /// <param name="assetsManager">Assets manager for resolving asset paths</param>
    /// <param name="meshFactory">Mesh factory for creating meshes</param>
    public static void Draw(MeshComponent meshComponent, IAssetsManager assetsManager, IMeshFactory meshFactory)
    {
        DragDropDrawer.HandleFileDropTarget(
            DragDropDrawer.ContentBrowserItemPayload,
            path => DragDropDrawer.HasValidExtension(path, SupportedExtensions),
            path =>
            {
                var fullPath = Path.Combine(assetsManager.AssetsPath, path);
                // TODO: laod model and set Mesh
            });
    }
}
