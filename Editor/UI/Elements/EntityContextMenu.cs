using Engine.Scene;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.UI.Elements;

public class EntityContextMenu
{
    public static void Render(IScene context)
    {
        if (ImGui.BeginPopupContextWindow("WindowContextMenu",
                ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
        {
            if (ImGui.MenuItem("Create Empty Entity"))
            {
                CreateEmptyEntity(context);
            }

            if (ImGui.MenuItem("Create 3D Entity"))
            {
                Create3DEntity(context);
            }

            ImGui.EndPopup();
        }
    }

    private static void CreateEmptyEntity(IScene context)
    {
        var entity = context.CreateEntity("Empty Entity");
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<IdComponent>();
    }

    private static void Create3DEntity(IScene context)
    {
        var entity = context.CreateEntity("3D Entity");
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<MeshComponent>();
        entity.AddComponent<ModelRendererComponent>();
    }
}