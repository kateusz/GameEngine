using ECS;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Panels;

public class SceneHierarchyPanel
{
    private Scene _context;
    private Entity _selectionContext;

    public SceneHierarchyPanel(Scene context)
    {
        _context = context;
    }

    public void SetContext(Scene context)
    {
        _context = context;
    }

    public void OnImGuiRender()
    {
        ImGui.Begin("Scene Hierarchy");

        foreach (var entity in Context.Instance.Entities)
        {
            DrawEntityNode(entity);
        }
        
        ImGui.End();
    }
    
    private void DrawEntityNode(Entity entity)
    {
        var tag = entity.Name;

        var flags = ((_selectionContext == entity) ? ImGuiTreeNodeFlags.Selected : 0) | ImGuiTreeNodeFlags.OpenOnArrow;
        var opened = ImGui.TreeNodeEx(tag, flags, tag);
        if (ImGui.IsItemClicked())
        {
            _selectionContext = entity;
        }

        if (opened)
        {
            flags = ImGuiTreeNodeFlags.OpenOnArrow;
            opened = ImGui.TreeNodeEx((IntPtr)9817239, flags, tag);
            if (opened)
                ImGui.TreePop();
            ImGui.TreePop();
        }

    }
}