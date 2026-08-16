using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using ImGuiNET;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors;

public class SkyLightComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<SkyLightComponent>(history)
{
    protected override string DisplayName => "Sky Light";

    protected override void DrawContent(SkyLightComponent component, Entity entity)
    {
        var path = component.HdrPath;

        UIPropertyRenderer.DrawPropertyRow("HDR Path", () =>
        {
            if (ImGui.InputText("##HdrPath", ref path, 512))
                component.HdrPath = path;
        });

        propertyRenderer.DrawPropertyField("Intensity", component.Intensity,
            newValue => component.Intensity = (float)newValue);
    }
}
