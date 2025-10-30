using System.Runtime.InteropServices;
using Engine;
using Engine.Renderer.Models;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.Elements;

public static class MeshDropTarget
{
    public static void Draw(MeshComponent meshComponent)
    {
        if (ImGui.BeginDragDropTarget())
        {
            unsafe
            {
                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
                if (payload.NativePtr != null)
                {
                    var path = Marshal.PtrToStringUni(payload.Data);
                    if (path is not null && path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        string fullPath = Path.Combine(AssetsManager.AssetsPath, path);
                        var mesh = MeshFactory.Create(fullPath);
                        mesh.Initialize();
                        meshComponent.SetMesh(mesh);
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }
    }
}