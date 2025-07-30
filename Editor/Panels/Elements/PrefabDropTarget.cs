using System.Runtime.InteropServices;
using ECS;
using Engine.Scene.Serializer;
using ImGuiNET;

namespace Editor.Panels.Elements;

public static class PrefabDropTarget
{
    public static void HandleEntityDrop(Entity entity)
    {
        if (ImGui.BeginDragDropTarget())
        {
            unsafe
            {
                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
                if (payload.NativePtr != null)
                {
                    var path = Marshal.PtrToStringUni(payload.Data);
                    if (path != null && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            string fullPath = Path.Combine(AssetsManager.AssetsPath, path);
                            PrefabSerializer.ApplyPrefabToEntity(entity, fullPath);
                            Console.WriteLine($"Applied prefab {path} to entity {entity.Name}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to apply prefab: {ex.Message}");
                        }
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }
    }
}