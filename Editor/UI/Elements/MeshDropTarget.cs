using Editor.UI.Drawers;
using Engine;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.VertexArray;
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
    /// <param name="meshFactory">Mesh factory for creating meshes</param>
    /// <param name="vertexArrayFactory">Factory for creating vertex arrays</param>
    /// <param name="vertexBufferFactory">Factory for creating vertex buffers</param>
    /// <param name="indexBufferFactory">Factory for creating index buffers</param>
    public static void Draw(MeshComponent meshComponent, IAssetsManager assetsManager, IMeshFactory meshFactory,
        IVertexArrayFactory vertexArrayFactory, IVertexBufferFactory vertexBufferFactory, IIndexBufferFactory indexBufferFactory)
    {
        DragDropDrawer.HandleFileDropTarget(
            DragDropDrawer.ContentBrowserItemPayload,
            path => DragDropDrawer.HasValidExtension(path, SupportedExtensions),
            path =>
            {
                string fullPath = Path.Combine(assetsManager.AssetsPath, path);
                var mesh = meshFactory.Create(fullPath);
                mesh.Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
                meshComponent.SetMesh(mesh);
            });
    }
}