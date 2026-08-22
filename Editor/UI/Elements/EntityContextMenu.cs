using Engine.Scene;
using ImGuiNET;
using SceneComponents;

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
    }

    private static void CreateEmptyEntity(IScene context)
    {
        _ = context.CreateEntity("Empty Entity");
    }
}
