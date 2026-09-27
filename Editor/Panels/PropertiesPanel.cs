using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.Features.Selection;
using Editor.UI;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;
using System.Numerics;
using GameComponentEditor = Editor.Features.Components.GameComponentEditor;

namespace Editor.Panels;

public class PropertiesPanel(
    IPrefabManager prefabManager,
    IComponentEditorRegistry componentEditors,
    ISceneContext sceneContext,
    GameComponentEditor gameComponentEditor,
    IEditorSelection selection,
    IEditorHistory history)
    : IPropertiesPanel, IEditorPanel
{
    private string _propertySearch = string.Empty;

    public void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(280, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Properties");
        DrawEntityProperties();
        ImGui.End();

        prefabManager.RenderPopups();
    }

    private void DrawEntityProperties()
    {
        var selected = selection.SelectedEntities;

        if (selected.Count == 0)
            return;

        PropertySearch.Filter = _propertySearch;
        try
        {
            if (selected.Count == 1)
            {
                DrawSingleEntity(selected[0]);
                return;
            }

            ImGui.TextUnformatted($"Multiple selection ({selected.Count})");
            try
            {
                MultiField.Targets = [.. selected];
                EntityNameEditor.Draw(selection.SelectedEntity!);
                DrawPropertySearchRow();
                ImGui.Spacing();
                componentEditors.DrawAllComponents(selection.SelectedEntity!);
            }
            finally
            {
                MultiField.Targets = null;
            }
        }
        finally
        {
            PropertySearch.Filter = null;
        }
    }

    private void DrawSingleEntity(Entity entity)
    {
        EntityNameEditor.Draw(entity);
        DrawPropertySearchRow();
        ImGui.Spacing();

        ComponentSelector.Draw(entity, sceneContext.ActiveScene!, gameComponentEditor, history);
        ImGui.SameLine();

        ButtonDrawer.DrawButton("Save as Prefab",
            () => prefabManager.ShowSavePrefabPopup(entity));

        componentEditors.DrawAllComponents(entity);
    }

    private void DrawPropertySearchRow()
    {
        ImGui.Columns(2, "property_search_columns", false);
        ImGui.SetColumnWidth(0, EditorUIConstants.DefaultColumnWidth);
        ImGui.Text("Search");
        ImGui.NextColumn();
        LayoutDrawer.DrawSearchInput("Filter properties...", ref _propertySearch);
        ImGui.Columns(1);
    }
}
