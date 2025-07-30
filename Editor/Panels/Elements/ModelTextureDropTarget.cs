using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer.Textures;
using ImGuiNET;

namespace Editor.Panels.Elements;

public static class ModelTextureDropTarget
{
    public static void Draw(string label, Action<Texture2D> onTextureChanged)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            if (ImGui.Button(label, new Vector2(-1, 0.0f)))
            {
                // Optional: Handle button click logic if needed
            }

            if (ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
                    if (payload.NativePtr != null)
                    {
                        var path = Marshal.PtrToStringUni(payload.Data);
                        if (path is not null)
                        {
                            string texturePath = Path.Combine(AssetsManager.AssetsPath, path);
                            if (File.Exists(texturePath) &&
                                (texturePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                 texturePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
                            {
                                onTextureChanged(TextureFactory.Create(texturePath));
                            }
                        }
                    }

                    ImGui.EndDragDropTarget();
                }
            }
        });
    }
}