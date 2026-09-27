using Editor.AssetPicker;

namespace Editor.UI.Elements;

/// <summary>
/// Texture asset field: opens Select Asset; Content Browser drop still supported.
/// </summary>
public class TextureDropTarget(AssetPathField assetPathField)
{
    public static readonly string[] SupportedExtensions = AssetKind.Texture.Extensions;

    public void Draw(string label, Action<string> onTexturePathChanged, string? currentTexturePath = null) =>
        assetPathField.Draw(label, AssetKind.Texture, currentTexturePath, onTexturePathChanged);
}
