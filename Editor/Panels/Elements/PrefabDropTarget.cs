using System.Runtime.InteropServices;
using ECS;
using Engine.Scene.Serializer;
using ImGuiNET;
using NLog;

namespace Editor.Panels.Elements;

public static class PrefabDropTarget
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
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
                            
                            // TODO: finish dependency injection
                            //PrefabSerializer.ApplyPrefabToEntity(entity, fullPath);
                            Logger.Info("Applied prefab {Path} to entity {EntityName}", path, entity.Name);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Failed to apply prefab");
                        }
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }
    }
}