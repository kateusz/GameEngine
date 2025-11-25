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
    private readonly IAssetsManager _assetsManager;

    public PrefabDropTarget(IPrefabSerializer prefabSerializer, IAssetsManager assetsManager)
    {
        _prefabSerializer = prefabSerializer;
        _assetsManager = assetsManager;
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
                            var fullPath = Path.Combine(_assetsManager.AssetsPath, path);
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