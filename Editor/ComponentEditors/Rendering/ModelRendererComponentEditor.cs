using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.UI.Elements;
using Engine.Renderer.Models;
using Engine.Renderer.Textures;
using Engine.Scene;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class ModelRendererComponentEditor(
    IModelFactory modelFactory,
    ITextureFactory textureFactory,
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
        }, textureFactory, component.TexturePath);
        propertyRenderer.DrawPropertyField("Tiling Factor", component.TilingFactor,
            newValue => component.TilingFactor = (float)newValue);
    }
}
