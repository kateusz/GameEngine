using System.Numerics;
using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using ImGuiNET;

namespace Editor.ComponentEditors.Core;

public class ComponentEditorRegistry(IEnumerable<IComponentEditor> editors) : IComponentEditorRegistry
{
    private readonly IComponentEditor[] _editors = [.. editors];

    public void DrawAllComponents(Entity entity)
    {
        foreach (var editor in _editors)
            editor.DrawComponent(entity);
    }

    public static void DrawComponent<T>(string name, Entity entity, Action uiFunction) where T : IComponent
    {
        if (!entity.TryGetComponent<T>(out _))
            return;

        DrawComponentTree(name, entity, typeof(T).GetHashCode().ToString(),
            () => entity.RemoveComponent<T>(), uiFunction, () => entity.TryGetComponent<T>(out _));
    }

    public static void DrawComponent(string name, Entity entity, Type componentType, Action uiFunction)
    {
        if (!entity.TryGetComponent(componentType, out _))
            return;

        DrawComponentTree(name, entity, $"{componentType.FullName}_{entity.Id}",
            () => entity.RemoveComponent(componentType), uiFunction,
            () => entity.TryGetComponent(componentType, out _));
    }

    private static void DrawComponentTree(
        string name,
        Entity entity,
        string treeNodeId,
        Action removeComponent,
        Action uiFunction,
        Func<bool> stillHasComponent)
    {
        var treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed
                                                           | ImGuiTreeNodeFlags.SpanAvailWidth |
                                                           ImGuiTreeNodeFlags.AllowOverlap |
                                                           ImGuiTreeNodeFlags.FramePadding;

        var contentRegionAvailable = ImGui.GetContentRegionAvail();

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
        var lineHeight = ImGui.GetFont().FontSize + ImGui.GetStyle().FramePadding.Y * 2.0f;
        ImGui.Separator();

        var open = ImGui.TreeNodeEx(treeNodeId, treeNodeFlags, name);
        ImGui.PopStyleVar();

        ImGui.SameLine(contentRegionAvailable.X - lineHeight * 0.5f);
        var removed = ButtonDrawer.DrawButton("-", lineHeight, lineHeight, removeComponent);

        if (!open)
            return;

        if (removed || !stillHasComponent())
        {
            ImGui.TreePop();
            return;
        }

        uiFunction();
        ImGui.TreePop();
    }
}