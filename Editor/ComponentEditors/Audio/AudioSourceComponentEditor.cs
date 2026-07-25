using Audio;
using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using ImGuiNET;
using SceneComponents.Audio;
using ZLinq;

namespace Editor.ComponentEditors.Audio;

public class AudioSourceComponentEditor(
    IAudioPlayback audioPlayback,
    AudioDropTarget audioDropTarget,
    UIPropertyRenderer propertyRenderer, IEditorHistory history) : ComponentEditor<AudioSourceComponent>(history)
{
    protected override string DisplayName => "Audio Source";

    protected override void DrawContent(AudioSourceComponent component, Entity entity)
    {
        audioDropTarget.Draw("Audio Clip", relativePath =>
        {
            component.AudioClipPath = relativePath;
        }, component.AudioClipPath);

        propertyRenderer.DrawPropertyField("Volume", component.Volume,
            newValue => component.Volume = System.Math.Clamp((float)newValue, 0.0f, 1.0f));
        propertyRenderer.DrawPropertyField("Pitch", component.Pitch,
            newValue => component.Pitch = System.Math.Clamp((float)newValue, 0.1f, 3.0f));
        propertyRenderer.DrawPropertyField("Loop", component.Loop,
            newValue => component.Loop = (bool)newValue);
        propertyRenderer.DrawPropertyField("Play On Awake", component.PlayOnAwake,
            newValue => component.PlayOnAwake = (bool)newValue);
        propertyRenderer.DrawPropertyField("Is 3D", component.Is3D,
            newValue => component.Is3D = (bool)newValue);

        if (component.Is3D)
        {
            LayoutDrawer.DrawIndentedSection(() =>
            {
                propertyRenderer.DrawPropertyField("Min Distance", component.MinDistance,
                    newValue => component.MinDistance = System.Math.Max((float)newValue, 0.1f));

                propertyRenderer.DrawPropertyField("Max Distance", component.MaxDistance,
                    newValue => component.MaxDistance = System.Math.Max((float)newValue, component.MinDistance));
            });
        }

        LayoutDrawer.DrawSeparatorWithSpacing();
        ImGui.Text("Playback Controls:");

        ButtonDrawer.DrawButton("Play", () => audioPlayback.Play(entity));

        DrawEffectsSection(component);
    }

    private static void DrawEffectsSection(AudioSourceComponent component)
    {
        LayoutDrawer.DrawSeparatorWithSpacing();

        if (!ImGui.CollapsingHeader("Effects"))
            return;
        
        if (ButtonDrawer.DrawButton("+ Add Effect"))
            ImGui.OpenPopup("AddEffectPopup");

        DrawAddEffectPopup(component);

        // Draw existing effects
        for (var i = component.Effects.Count - 1; i >= 0; i--)
        {
            var effect = component.Effects[i];
            ImGui.PushID(i);

            var enabled = effect.Enabled;
            if (ImGui.Checkbox("##enabled", ref enabled))
                effect.Enabled = enabled;

            ImGui.SameLine();
            ImGui.Text(effect.Type.ToString());

            ImGui.SameLine();
            if (ButtonDrawer.DrawColoredButton("X", MessageType.Error))
            {
                component.Effects.RemoveAt(i);
                ImGui.PopID();
                continue;
            }

            if (effect.Enabled)
            {
                var amount = effect.Amount;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.SliderFloat("##amount", ref amount, 0f, 1f, "%.2f"))
                    effect.Amount = amount;
            }

            ImGui.PopID();
            ImGui.Spacing();
        }
    }

    private static void DrawAddEffectPopup(AudioSourceComponent component)
    {
        if (!ImGui.BeginPopup("AddEffectPopup"))
            return;

        foreach (var type in Enum.GetValues<AudioEffectType>())
        {
            // Skip if already has this effect type
            if (component.Effects.AsValueEnumerable().Any(e => e.Type == type))
                continue;

            if (ImGui.Selectable(type.ToString()))
            {
                component.Effects.Add(new AudioEffectData
                {
                    Type = type,
                    Enabled = true,
                    Amount = 0.5f
                });
            }
        }

        ImGui.EndPopup();
    }
}