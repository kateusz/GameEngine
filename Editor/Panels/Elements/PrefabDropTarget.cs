using System.Runtime.InteropServices;
using ECS;
using Engine.Scene.Serializer;
using ImGuiNET;
using Serilog;

namespace Editor.Panels.Elements;

public static class PrefabDropTarget
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(PrefabDropTarget));
    
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
                            Logger.Information("Applied prefab {Path} to entity {EntityName}", path, entity.Name);
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