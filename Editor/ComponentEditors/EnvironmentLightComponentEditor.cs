using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Core;
using Engine.Renderer.Textures;
using ImGuiNET;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors;

public class EnvironmentLightComponentEditor(ITextureFactory textureFactory, UIPropertyRenderer propertyRenderer)
    : ComponentEditor<EnvironmentLightComponent>
{
    protected override string DisplayName => "Environment Light";

    protected override void DrawContent(EnvironmentLightComponent component, Entity entity)
    {
        TryLoadHdr(component.HdrPath);

        HdrDropTarget.Draw(
            "HDR Map",
            relativePath =>
            {
                component.HdrPath = relativePath;
                TryLoadHdr(relativePath);
            },
            textureFactory,
            component.HdrPath);

        propertyRenderer.DrawPropertyField("Exposure", component.Exposure,
            newValue => component.Exposure = System.Math.Clamp((float)newValue, 0.01f, 16f));

        if (!string.IsNullOrEmpty(component.HdrPath))
            TextDrawer.DrawInfoText($"Skybox: {Path.GetFileName(component.HdrPath)}");
        else
            TextDrawer.DrawInfoText("Drop a .hdr file from the Content Browser.");
    }

    private void TryLoadHdr(string? relativeOrAbsolutePath)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
            return;

        try
        {
            var resolved = PathBuilder.Resolve(relativeOrAbsolutePath);
            textureFactory.Create(resolved);
        }
        catch (Exception ex)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.4f, 0.3f, 1f), $"HDR load failed: {ex.Message}");
        }
    }
}
