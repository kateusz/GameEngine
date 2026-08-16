using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using Engine.Renderer.Textures;
using ImGuiNET;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class ModelRendererComponentEditor(
    ITextureFactory textureFactory,
    UIPropertyRenderer propertyRenderer,
    IEditorHistory history) : ComponentEditor<ModelRendererComponent>(history)
{
    protected override string DisplayName => "Model Renderer";

    protected override void DrawContent(ModelRendererComponent component, Entity entity)
    {
        MeshDropTarget.Draw("Model", path => component.ModelPath = path, component.ModelPath);
        TextureDropTarget.Draw("Albedo", path => component.AlbedoTexturePath = path, textureFactory, component.AlbedoTexturePath);
        propertyRenderer.DrawPropertyField("Color", component.Color,
            newValue => component.Color = (System.Numerics.Vector4)newValue);

        DrawOptional01("Metallic Override", component.MetallicOverride, v => component.MetallicOverride = v);
        DrawOptional01("Roughness Override", component.RoughnessOverride, v => component.RoughnessOverride = v);
    }

    private static void DrawOptional01(string label, float? value, Action<float?> set)
    {
        var enabled = value.HasValue;
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            if (ImGui.Checkbox($"##{label}Enabled", ref enabled))
                set(enabled ? System.Math.Clamp(value ?? 0.5f, 0f, 1f) : null);

            if (!enabled)
                return;

            ImGui.SameLine();
            var v = value ?? 0.5f;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat($"##{label}Value", ref v, 0f, 1f))
                set(System.Math.Clamp(v, 0f, 1f));
        });
    }
}
