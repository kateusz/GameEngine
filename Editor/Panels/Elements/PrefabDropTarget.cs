using System.Runtime.InteropServices;
using ECS;
using Engine;
using Engine.Scene.Serializer;
using ImGuiNET;
using Serilog;

namespace Editor.Panels.Elements;

public class PrefabDropTarget
{
    private static readonly ILogger Logger = Log.ForContext(typeof(PrefabDropTarget));

    private readonly IPrefabSerializer _prefabSerializer;

    public PrefabDropTarget(IPrefabSerializer prefabSerializer)
    {
        _prefabSerializer = prefabSerializer;
    }

    public void HandleEntityDrop(Entity entity)
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
                            var fullPath = Path.Combine(AssetsManager.AssetsPath, path);
                            _prefabSerializer.ApplyPrefabToEntity(entity, fullPath);
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