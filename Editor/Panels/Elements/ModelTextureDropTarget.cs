using System.Numerics;
using System.Runtime.InteropServices;
using Engine;
using Engine.Renderer.Textures;
using ImGuiNET;

namespace Editor.Panels.Elements;

public static class ModelTextureDropTarget
{
    /// <summary>
    /// Renders a button that accepts texture file drops from the content browser.
    /// </summary>
    /// <param name="label">Label for the button</param>
    /// <param name="onTextureChanged">Callback invoked when a texture is dropped</param>
    /// <param name="assetsManager">Assets manager for resolving asset paths</param>
    public static void Draw(string label, Action<Texture2D> onTextureChanged, IAssetsManager assetsManager)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            RenderDropTargetButton(label);
            HandleTextureDragDrop(onTextureChanged, assetsManager);
        });
    }

    private static void RenderDropTargetButton(string label)
    {
        if (ImGui.Button(label, new Vector2(-1, 0.0f)))
        {
            // Optional: Handle button click logic if needed
        }
    }

    private static void HandleTextureDragDrop(Action<Texture2D> onTextureChanged, IAssetsManager assetsManager)
    {
        if (!ImGui.BeginDragDropTarget())
        {
            return;
        }

        ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
        ProcessTexturePayload(payload, onTextureChanged, assetsManager);
        ImGui.EndDragDropTarget();
    }

    private static unsafe void ProcessTexturePayload(
        ImGuiPayloadPtr payload,
        Action<Texture2D> onTextureChanged,
        IAssetsManager assetsManager)
    {
        if (payload.NativePtr == null)
        {
            return;
        }

        var path = Marshal.PtrToStringUni(payload.Data);
        if (path is null)
        {
            return;
        }

        string texturePath = Path.Combine(assetsManager.AssetsPath, path);
        if (!IsValidTextureFile(texturePath))
        {
            return;
        }

        onTextureChanged(TextureFactory.Create(texturePath));
    }

    private static bool IsValidTextureFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        return filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
               filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase);
    }
}