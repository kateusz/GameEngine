using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.UI.Elements;
using Engine.Renderer.Models;
using Engine.Scene;
using ImGuiNET;
using SceneComponents;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class ModelRendererComponentEditor(
    IModelFactory modelFactory,
    UIPropertyRenderer propertyRenderer,
    IEditorHistory history,
    ISceneContext sceneContext) : ComponentEditor<ModelRendererComponent>(history)
{
    protected override string DisplayName => "Model Renderer";

    protected override void DrawContent(ModelRendererComponent component, Entity entity)
    {
        ModelDropTarget.Draw("Model", (relativePath, model) =>
        {
            var scene = sceneContext.ActiveScene;
            if (scene == null)
            {
                component.ModelPath = relativePath;
                component.MeshIndex = null;
                component.SuppressDraw = false;
                return;
            }

            history.Execute(new ImportModelHierarchyCommand(scene, entity, component, model, relativePath));
        }, modelFactory, component.ModelPath);
        propertyRenderer.DrawPropertyField("Color", component.Color,
            newValue => component.Color = (System.Numerics.Vector4)newValue);
        TextureDropTarget.Draw("Texture", relativePath =>
        {
            component.TexturePath = relativePath;
        }, component.TexturePath);
        propertyRenderer.DrawPropertyField("Tiling Factor", component.TilingFactor,
            newValue => component.TilingFactor = (float)newValue);
        propertyRenderer.DrawPropertyField("Metallic", component.Metallic,
            newValue => component.Metallic = (float)newValue);
        propertyRenderer.DrawPropertyField("Roughness", component.Roughness,
            newValue => component.Roughness = (float)newValue);
        propertyRenderer.DrawPropertyField("AO", component.Ao,
            newValue => component.Ao = (float)newValue);
        if (!string.IsNullOrWhiteSpace(component.ModelPath))
        {
            propertyRenderer.DrawPropertyField("Override Material", component.OverrideMaterial,
                newValue => component.OverrideMaterial = (bool)newValue);
        }

        DrawVisibilityZonePicker(component);
    }

    private void DrawVisibilityZonePicker(ModelRendererComponent component)
    {
        var scene = sceneContext.ActiveScene;
        if (scene == null)
            return;

        var currentLabel = "(none)";
        if (component.VisibilityZoneEntityId >= 0)
        {
            currentLabel = scene.Context.Contains(component.VisibilityZoneEntityId)
                ? scene.Context.GetById(component.VisibilityZoneEntityId).Name
                : $"Missing #{component.VisibilityZoneEntityId}";
        }

        if (ImGui.BeginCombo("Visibility Zone", currentLabel))
        {
            if (ImGui.Selectable("(none)", component.VisibilityZoneEntityId < 0))
                component.VisibilityZoneEntityId = -1;

            foreach (var (zoneEntity, _, _) in scene.Context.View<VisibilityZoneComponent, TransformComponent>())
            {
                if (ImGui.Selectable(zoneEntity.Name, component.VisibilityZoneEntityId == zoneEntity.Id))
                    component.VisibilityZoneEntityId = zoneEntity.Id;
            }

            ImGui.EndCombo();
        }
    }
}
