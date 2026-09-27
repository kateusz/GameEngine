using System.Numerics;
using ECS;
using Editor.ComponentEditors.Core;

using Editor.Features.History;

using Editor.Features.Selection;

using Editor.UI.Drawers;

using Editor.UI.Elements;

using Engine.Scene;

using ImGuiNET;

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

        {

            DrawSceneProperties();

            return;

        }



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

            ImGui.Spacing();

            componentEditors.DrawAllComponents(selection.SelectedEntity!);

        }

        finally

        {

            MultiField.Targets = null;

        }

    }



    private void DrawSingleEntity(Entity entity)

    {

        DrawSceneProperties();



        EntityNameEditor.Draw(entity);

        ImGui.Spacing();



        ComponentSelector.Draw(entity, sceneContext.ActiveScene!, gameComponentEditor, history);

        ImGui.SameLine();



        ButtonDrawer.DrawButton("Save as Prefab",

            () => prefabManager.ShowSavePrefabPopup(entity));



        componentEditors.DrawAllComponents(entity);

    }



    private void DrawSceneProperties()

    {

        if (sceneContext.ActiveScene is not { } scene)

            return;



        ImGui.SeparatorText("Scene");



        var backgroundColor = scene.BackgroundColor;

        if (ImGui.ColorEdit4("Background Color", ref backgroundColor,

                ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.InputRGB |

                ImGuiColorEditFlags.NoOptions))

        {

            scene.BackgroundColor = backgroundColor;

        }

    }

}


