using Editor.UI.Drawers;
using Engine.Core;
using Engine.Renderer.Textures;
using Engine.Scene.Serializer;

namespace Editor.UI.Elements;

/// <summary>
/// UI element that provides drag-and-drop functionality for texture files.
/// Allows users to drag texture files (.png, .jpg) from the content browser onto texture properties.
/// </summary>
public static class TextureDropTarget
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg"];

    /// <summary>
    /// Draws a drag-and-drop target button for textures.
    /// </summary>
    /// <param name="label">Label to display for the property</param>
    /// <param name="onTexturePathChanged">Callback invoked with an asset-relative path when a texture is dropped</param>
    /// <param name="textureFactory">Texture factory for creating textures</param>
    public static void Draw(string label, Action<string> onTexturePathChanged, ITextureFactory textureFactory)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            ButtonDrawer.DrawFullWidthButton(label, () =>
            {
                // Optional: Handle button click logic if needed
            });

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path =>
                {
                    var texturePath = PathBuilder.Resolve(path);
                    return DragDropDrawer.IsValidFile(texturePath, SupportedExtensions);
                },
                path =>
                {
                    var texturePath = PathBuilder.Resolve(path);
                    textureFactory.Create(texturePath);
                    onTexturePathChanged(ToAssetRelativePath(path));
                });
        });
    }

    private static string ToAssetRelativePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            var assetsPath = AssetsManager.AssetsPath;
            if (path.StartsWith(assetsPath, StringComparison.OrdinalIgnoreCase))
                return Path.GetRelativePath(assetsPath, path).Replace('\\', '/');
            return path;
        }

        return path.Replace('\\', '/');
    }
}
