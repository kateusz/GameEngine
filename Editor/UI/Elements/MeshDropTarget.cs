using Editor.UI.Drawers;
using Engine;
using Engine.Renderer;
using Engine.Scene.Components;

namespace Editor.UI.Elements;

/// <summary>
/// UI element that provides drag-and-drop functionality for mesh files.
/// Allows users to drag mesh files (.obj) from the content browser onto mesh properties.
/// </summary>
public static class MeshDropTarget
{
    private static readonly string[] SupportedExtensions = [".obj"];

    /// <summary>
    /// Draws a drag-and-drop target for mesh files.
    /// </summary>
    /// <param name="meshComponent">Mesh component to update when a mesh is dropped</param>
    /// <param name="assetsManager">Assets manager for resolving asset paths</param>
    public static void Draw(MeshComponent meshComponent, IAssetsManager assetsManager)
    {
        DragDropDrawer.HandleFileDropTarget(
            DragDropDrawer.ContentBrowserItemPayload,
            path => DragDropDrawer.HasValidExtension(path, SupportedExtensions),
            path =>
            {
                string fullPath = Path.Combine(assetsManager.AssetsPath, path);
                var mesh = MeshFactory.Create(fullPath);
                mesh.Initialize();
                meshComponent.SetMesh(mesh);
            });
    }
}