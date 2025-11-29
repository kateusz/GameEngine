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
    /// <param name="onTextureChanged">Callback invoked when a texture is dropped</param>
    /// <param name="assetsManager">Assets manager for resolving asset paths</param>
    /// <param name="textureFactory">Texture factory for creating textures</param>
    public static void Draw(string label, Action<Texture2D> onTextureChanged, IAssetsManager assetsManager, ITextureFactory textureFactory)
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
                    var texturePath = Path.Combine(assetsManager.AssetsPath, path);
                    return DragDropDrawer.IsValidFile(texturePath, SupportedExtensions);
                },
                path =>
                {
                    var texturePath = Path.Combine(assetsManager.AssetsPath, path);
                    onTextureChanged(textureFactory.Create(texturePath));
                });
        });
    }
}