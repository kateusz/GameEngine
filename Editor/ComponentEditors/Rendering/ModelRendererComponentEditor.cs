using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Models;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;
using SceneComponents;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class ModelRendererComponentEditor(
    EditorModelLoadService modelLoadService,
    UIPropertyRenderer propertyRenderer,
    IEditorHistory history,
    ISceneContext sceneContext) : ComponentEditor<ModelRendererComponent>(history)
{
    protected override string DisplayName => "Model Renderer";

    protected override void DrawContent(ModelRendererComponent component, Entity entity)
    {
        string? modelPath = null;
        if (MultiField.TryUniform(entity,
                e => e.GetComponent<ModelRendererComponent>().ModelPath,
                (a, b) => string.Equals(a, b, StringComparison.Ordinal),
                out var uniformPath))
            modelPath = uniformPath;

        ModelDropTarget.Draw("Model", (relativePath, model) =>
        {
            if (MultiField.Targets is not null)
            {
                MultiField.WriteEach(entity, (Entity e, string path) =>
                {
                    var c = e.GetComponent<ModelRendererComponent>();
                    c.ModelPath = path;
                    c.MeshIndex = null;
                    c.SuppressDraw = false;
                }, relativePath);
                return;
            }

            var scene = sceneContext.ActiveScene;
            if (scene == null)
            {
                component.ModelPath = relativePath;
                component.MeshIndex = null;
                component.SuppressDraw = false;
                return;
            }

            history.Execute(new ImportModelHierarchyCommand(scene, entity, component, model, relativePath));
        }, modelLoadService, modelPath);

        var hasModel = MultiField.Targets is null
            ? !string.IsNullOrWhiteSpace(component.ModelPath)
            : MultiField.For(entity).All(e =>
                !string.IsNullOrWhiteSpace(e.GetComponent<ModelRendererComponent>().ModelPath));

        if (!hasModel)
        {
            DrawVisibilityZonePicker(entity);
            return;
        }

        propertyRenderer.DrawPropertyField("Color", entity,
            e => e.GetComponent<ModelRendererComponent>().Color,
            (e, v) => e.GetComponent<ModelRendererComponent>().Color = v,
            UIPropertyRenderer.SameVector4);

        string? texturePath = null;
        if (MultiField.TryUniform(entity,
                e => e.GetComponent<ModelRendererComponent>().TexturePath,
                (a, b) => string.Equals(a, b, StringComparison.Ordinal),
                out var uniformTexture))
            texturePath = uniformTexture;

        TextureDropTarget.Draw("Texture", relativePath =>
        {
            MultiField.WriteEach(entity, (Entity e, string path) =>
            {
                e.GetComponent<ModelRendererComponent>().TexturePath = path;
            }, relativePath);
        }, texturePath);

        propertyRenderer.DrawPropertyField("Tiling Factor", entity,
            e => e.GetComponent<ModelRendererComponent>().TilingFactor,
            (e, v) => e.GetComponent<ModelRendererComponent>().TilingFactor = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Override Material", entity,
            e => e.GetComponent<ModelRendererComponent>().OverrideMaterial,
            (e, v) => e.GetComponent<ModelRendererComponent>().OverrideMaterial = v);

        var overrideOn = MultiField.TryUniform(entity,
            e => e.GetComponent<ModelRendererComponent>().OverrideMaterial,
            EqualityComparer<bool>.Default.Equals,
            out var overrideMaterial) && overrideMaterial;

        if (overrideOn)
        {
            propertyRenderer.DrawPropertyField("Metallic", entity,
                e => e.GetComponent<ModelRendererComponent>().Metallic,
                (e, v) => e.GetComponent<ModelRendererComponent>().Metallic = v,
                MultiField.SameFloat);

            propertyRenderer.DrawPropertyField("Roughness", entity,
                e => e.GetComponent<ModelRendererComponent>().Roughness,
                (e, v) => e.GetComponent<ModelRendererComponent>().Roughness = v,
                MultiField.SameFloat);

            propertyRenderer.DrawPropertyField("AO", entity,
                e => e.GetComponent<ModelRendererComponent>().Ao,
                (e, v) => e.GetComponent<ModelRendererComponent>().Ao = v,
                MultiField.SameFloat);
        }

        DrawVisibilityZonePicker(entity);
    }

    private void DrawVisibilityZonePicker(Entity entity)
    {
        var scene = sceneContext.ActiveScene;
        if (scene == null)
            return;

        var currentLabel = "";
        if (MultiField.TryUniform(entity,
                e => e.GetComponent<ModelRendererComponent>().VisibilityZoneEntityId,
                EqualityComparer<int>.Default.Equals,
                out var zoneId))
        {
            currentLabel = zoneId < 0
                ? "(none)"
                : scene.Context.Contains(zoneId)
                    ? scene.Context.GetById(zoneId).Name
                    : $"Missing #{zoneId}";
        }

        UIPropertyRenderer.DrawPropertyRow("Visibility Zone", () =>
        {
            if (ImGui.BeginCombo("##VisibilityZone", currentLabel))
            {
                if (ImGui.Selectable("(none)", currentLabel == "(none)"))
                {
                    MultiField.WriteEach(entity, (Entity e, int id) =>
                    {
                        e.GetComponent<ModelRendererComponent>().VisibilityZoneEntityId = id;
                    }, -1);
                }

                foreach (var (zoneEntity, _, _) in scene.Context.View<VisibilityZoneComponent, TransformComponent>())
                {
                    if (ImGui.Selectable(zoneEntity.Name, currentLabel == zoneEntity.Name))
                    {
                        MultiField.WriteEach(entity, (Entity e, int id) =>
                        {
                            e.GetComponent<ModelRendererComponent>().VisibilityZoneEntityId = id;
                        }, zoneEntity.Id);
                    }
                }

                ImGui.EndCombo();
            }
        });
    }
}
