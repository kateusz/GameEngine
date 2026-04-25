using System.Numerics;
using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Scene.Components;
using Engine.Scene.Components.Lights;
using ImGuiNET;

namespace Editor.ComponentEditors.Core;

public class ComponentEditorRegistry(
    ComponentEditorCollection editorCollection,
    ScriptComponentEditor scriptComponentEditor)
    : IComponentEditorRegistry
{
    private readonly IReadOnlyDictionary<Type, IComponentEditor> _editors = editorCollection.Editors;

    public void DrawAllComponents(Entity entity)
    {
        foreach (var (_, editor) in _editors)
        {
            editor.DrawComponent(entity);
        }

        // Special handling for script components
        scriptComponentEditor.DrawScriptComponent(entity);
    }

    public static void DrawComponent<T>(string name, Entity entity, Action uiFunction) where T : IComponent
    {
        var treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed
                                                           | ImGuiTreeNodeFlags.SpanAvailWidth |
                                                           ImGuiTreeNodeFlags.AllowOverlap |
                                                           ImGuiTreeNodeFlags.FramePadding;

        if (entity.TryGetComponent<T>(out _))
        {
            var contentRegionAvailable = ImGui.GetContentRegionAvail();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
            var lineHeight = ImGui.GetFont().FontSize + ImGui.GetStyle().FramePadding.Y * 2.0f;
            ImGui.Separator();

            var open = ImGui.TreeNodeEx(typeof(T).GetHashCode().ToString(), treeNodeFlags, name);
            ImGui.PopStyleVar();

            ImGui.SameLine(contentRegionAvailable.X - lineHeight * 0.5f);
            var removed = ButtonDrawer.DrawButton("-", lineHeight, lineHeight, entity.RemoveComponent<T>);

            if (open)
            {
                if (removed || !entity.TryGetComponent<T>(out _))
                {
                    ImGui.TreePop();
                    return;
                }

                uiFunction();
                ImGui.TreePop();
            }
        }
    }
}