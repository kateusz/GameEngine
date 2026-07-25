using Engine.Scene;
using ImGuiNET;
using SceneComponents;
using SceneComponents.Rendering;

namespace Editor.UI.Elements;

public interface IEntityContextMenu
{
    void Render(IScene context);

    /// <summary>Context menu for the last submitted item (e.g. hierarchy empty-space drop target).</summary>
    void RenderForLastItem(IScene context);
}

public class EntityContextMenu : IEntityContextMenu
{
    public void Render(IScene context)
    {
        if (ImGui.BeginPopupContextWindow("WindowContextMenu",
                ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
        {
            DrawCreateItems(context);
            ImGui.EndPopup();
        }
    }

    public void RenderForLastItem(IScene context)
    {
        if (ImGui.BeginPopupContextItem("HierarchyBgContextMenu"))
        {
            DrawCreateItems(context);
            ImGui.EndPopup();
        }
    }

    private static void DrawCreateItems(IScene context)
    {
        if (ImGui.MenuItem("Create Empty Entity"))
            CreateEmptyEntity(context);

        if (ImGui.MenuItem("Create 3D Entity"))
            Create3DEntity(context);
    }

    private static void CreateEmptyEntity(IScene context)
    {
        _ = context.CreateEntity("Empty Entity");
    }

    private static void Create3DEntity(IScene context)
    {
        var entity = context.CreateEntity("3D Entity");
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ModelRendererComponent>();
    }
}
