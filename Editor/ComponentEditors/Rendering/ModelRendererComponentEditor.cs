using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Models;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using SceneComponents;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class ModelRendererComponentEditor(
    EditorModelLoadService modelLoadService,
    ModelDropTarget modelDropTarget,
    TextureDropTarget textureDropTarget,
    UIPropertyRenderer propertyRenderer,
    IEditorHistory history,
    ISceneContext sceneContext) : ComponentEditor<ModelRendererComponent>(history)
{
    protected override string DisplayName => "Model Renderer";

    protected override void DrawContent(ModelRendererComponent component, Entity entity)
    {
        modelDropTarget.Draw("Model", (relativePath, model) =>
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
            }, modelLoadService,
            MultiField.UniformPath(entity, e => e.GetComponent<ModelRendererComponent>().ModelPath));

        DrawMaterialFields(entity);
        DrawVisibilityZonePicker(entity);
    }

    private void DrawMaterialFields(Entity entity)
    {
        propertyRenderer.DrawPropertyField("Color", entity,
            e => e.GetComponent<ModelRendererComponent>().Color,
            (e, v) => e.GetComponent<ModelRendererComponent>().Color = v,
            UIPropertyRenderer.SameVector4);

        textureDropTarget.Draw("Texture",
            relativePath => MultiField.WriteEach(entity,
                (Entity e, string path) => { e.GetComponent<ModelRendererComponent>().TexturePath = path; },
                relativePath),
            MultiField.UniformPath(entity, e => e.GetComponent<ModelRendererComponent>().TexturePath));

        propertyRenderer.DrawPropertyField("Tiling Factor", entity,
            e => e.GetComponent<ModelRendererComponent>().TilingFactor,
            (e, v) => e.GetComponent<ModelRendererComponent>().TilingFactor = v,
            MultiField.SameFloat);

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
            var zones = new List<(int Id, string Name)>();
            foreach (var (zoneEntity, _, _) in scene.Context.View<VisibilityZoneComponent, TransformComponent>())
                zones.Add((zoneEntity.Id, zoneEntity.Name));

            LayoutDrawer.DrawEntityIdCombo("##VisibilityZone", currentLabel,
                selectedId =>
                {
                    MultiField.WriteEach(entity,
                        (Entity e, int id) => { e.GetComponent<ModelRendererComponent>().VisibilityZoneEntityId = id; },
                        selectedId);
                }, zones);
        });
    }
}