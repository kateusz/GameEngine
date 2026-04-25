using System.Numerics;
using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Engine.Scene.Serializer;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class ModelRendererComponentEditor(ITextureFactory textureFactory) : IComponentEditor
{
    private static readonly ImGuiColorEditFlags ColorFlags =
        ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB |
        ImGuiColorEditFlags.InputRGB | ImGuiColorEditFlags.NoOptions;

    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<ModelRendererComponent>("Model Renderer", entity, () =>
        {
            var mr = entity.GetComponent<ModelRendererComponent>();

            UIPropertyRenderer.DrawPropertyField("Cast Shadows", mr.CastShadows,
                v => mr.CastShadows = (bool)v);
            UIPropertyRenderer.DrawPropertyField("Receive Shadows", mr.ReceiveShadows,
                v => mr.ReceiveShadows = (bool)v);

            if (mr.Materials.Count == 0)
                return;

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextDisabled($"Materials ({mr.Materials.Count})");

            for (var i = 0; i < mr.Materials.Count; i++)
            {
                var material = mr.Materials[i];
                DrawMaterial(material, i);
            }
        });
    }

    private void DrawMaterial(PbrMaterial material, int index)
    {
        if (!ImGui.CollapsingHeader($"Material {index}##mat{index}"))
            return;

        ImGui.PushID(index);

        // --- Textures ---
        DrawTextureSlot("Base Color", material.BaseColorTexturePath, (tex, path) =>
        {
            material.BaseColorTexture = tex;
            material.BaseColorTexturePath = path;
        });

        DrawTextureSlot("Metallic Roughness", material.MetallicRoughnessTexturePath, (tex, path) =>
        {
            material.MetallicRoughnessTexture = tex;
            material.MetallicRoughnessTexturePath = path;
        });

        DrawTextureSlot("Normal", material.NormalTexturePath, (tex, path) =>
        {
            material.NormalTexture = tex;
            material.NormalTexturePath = path;
        });

        DrawTextureSlot("AO", material.AoTexturePath, (tex, path) =>
        {
            material.AoTexture = tex;
            material.AoTexturePath = path;
        });

        DrawTextureSlot("Emissive", material.EmissiveTexturePath, (tex, path) =>
        {
            material.EmissiveTexture = tex;
            material.EmissiveTexturePath = path;
        });

        // --- Factors ---
        ImGui.Spacing();
        ImGui.TextDisabled("Factors");

        var baseColorFactor = new Vector4(material.BaseColorFactor.X, material.BaseColorFactor.Y,
            material.BaseColorFactor.Z, material.BaseColorFactor.W);
        if (ImGui.ColorEdit4("Base Color Factor", ref baseColorFactor, ColorFlags))
            material.BaseColorFactor = baseColorFactor;

        var metallic = material.MetallicFactor;
        if (ImGui.SliderFloat("Metallic", ref metallic, 0.0f, 1.0f, "%.3f"))
            material.MetallicFactor = metallic;

        var roughness = material.RoughnessFactor;
        if (ImGui.SliderFloat("Roughness", ref roughness, 0.0f, 1.0f, "%.3f"))
            material.RoughnessFactor = roughness;

        var emissiveFactor = new Vector3(material.EmissiveFactor.X, material.EmissiveFactor.Y, material.EmissiveFactor.Z);
        if (ImGui.ColorEdit3("Emissive Factor", ref emissiveFactor, ColorFlags | ImGuiColorEditFlags.HDR))
            material.EmissiveFactor = emissiveFactor;

        var normalScale = material.NormalScale;
        if (ImGui.SliderFloat("Normal Scale", ref normalScale, 0.0f, 2.0f, "%.3f"))
            material.NormalScale = normalScale;

        var aoStrength = material.AoStrength;
        if (ImGui.SliderFloat("AO Strength", ref aoStrength, 0.0f, 1.0f, "%.3f"))
            material.AoStrength = aoStrength;

        ImGui.PopID();
    }

    private static readonly string[] TextureExtensions = [".png", ".jpg", ".jpeg", ".dds", ".tga"];

    private void DrawTextureSlot(string label, string? currentPath, Action<Texture2D, string> onChanged)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var displayName = currentPath != null ? System.IO.Path.GetFileName(currentPath) : "None";
            ButtonDrawer.DrawFullWidthButton(displayName, () => { });

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path => DragDropDrawer.IsValidFile(PathBuilder.Build(path), TextureExtensions),
                path =>
                {
                    var full = PathBuilder.Build(path);
                    var tex = textureFactory.Create(full);
                    onChanged(tex, full);
                });
        });
    }
}
