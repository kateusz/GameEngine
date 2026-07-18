using Editor.UI.Drawers;
using Engine.Core;
using Engine.Renderer.Textures;

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
    /// <param name="currentTexturePath">Currently assigned texture path (can be null)</param>
    public static void Draw(string label, Action<string> onTexturePathChanged, ITextureFactory textureFactory, string? currentTexturePath = null)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var buttonLabel = !string.IsNullOrEmpty(currentTexturePath)
                ? Path.GetFileName(currentTexturePath)
                : label;

            ButtonDrawer.DrawFullWidthButton(buttonLabel, () =>
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
                    onTexturePathChanged(PathBuilder.ToAssetRelativePath(path));
                });
        });
    }
}
