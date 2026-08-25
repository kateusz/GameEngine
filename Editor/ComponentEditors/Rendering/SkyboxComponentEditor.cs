using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Core;
using Engine.Renderer.Textures;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class SkyboxComponentEditor(
    ITextureFactory textureFactory,
    UIPropertyRenderer propertyRenderer,
    IEditorHistory history) : ComponentEditor<SkyboxComponent>(history)
{
    private static readonly string[] HdrExtensions = [".hdr"];

    protected override string DisplayName => "Skybox";

    protected override void DrawContent(SkyboxComponent component, Entity entity)
    {
        UIPropertyRenderer.DrawPropertyRow("HDR", () =>
        {
            var buttonLabel = !string.IsNullOrEmpty(component.HdrPath)
                ? Path.GetFileName(component.HdrPath)
                : "HDR";

            ButtonDrawer.DrawFullWidthButton(buttonLabel, () => { });

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path =>
                {
                    var resolved = PathBuilder.Resolve(path);
                    return DragDropDrawer.IsValidFile(resolved, HdrExtensions);
                },
                path =>
                {
                    var resolved = PathBuilder.Resolve(path);
                    textureFactory.Create(resolved, sRgb: false);
                    component.HdrPath = PathBuilder.ToAssetRelativePath(path);
                });
        });

        propertyRenderer.DrawPropertyField("Intensity", component.Intensity,
            newValue => component.Intensity = (float)newValue);
    }
}
