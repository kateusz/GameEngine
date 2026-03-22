using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Audio;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class AudioSourceComponentEditor(IAudioEngine audioEngine, AudioDropTarget audioDropTarget) : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<AudioSourceComponent>("Audio Source", entity, () =>
        {
            var component = entity.GetComponent<AudioSourceComponent>();
            audioDropTarget.Draw("Audio Clip", component.AudioClip, audioClip =>
            {
                component.AudioClip = audioClip;
                component.AudioClipPath = audioClip.Path;
            });

            UIPropertyRenderer.DrawPropertyField("Volume", component.Volume,
                newValue => component.Volume = System.Math.Clamp((float)newValue, 0.0f, 1.0f));
            UIPropertyRenderer.DrawPropertyField("Pitch", component.Pitch,
                newValue => component.Pitch = System.Math.Clamp((float)newValue, 0.1f, 3.0f));
            UIPropertyRenderer.DrawPropertyField("Loop", component.Loop,
                newValue => component.Loop = (bool)newValue);
            UIPropertyRenderer.DrawPropertyField("Play On Awake", component.PlayOnAwake,
                newValue => component.PlayOnAwake = (bool)newValue);
            UIPropertyRenderer.DrawPropertyField("Is 3D", component.Is3D,
                newValue => component.Is3D = (bool)newValue);

            if (component.Is3D)
            {
                LayoutDrawer.DrawIndentedSection(() =>
                {
                    UIPropertyRenderer.DrawPropertyField("Min Distance", component.MinDistance,
                        newValue => component.MinDistance = System.Math.Max((float)newValue, 0.1f));

                    UIPropertyRenderer.DrawPropertyField("Max Distance", component.MaxDistance,
                        newValue => component.MaxDistance = System.Math.Max((float)newValue, component.MinDistance));
                });
            }

            LayoutDrawer.DrawSeparatorWithSpacing();
            ImGui.Text("Playback Controls:");

            ButtonDrawer.DrawButton("Play", () =>
            {
                var path = component.AudioClip?.Path;
                if (!string.IsNullOrWhiteSpace(path))
                    audioEngine.PlayOneShot(path, volume: 0.5f);
            });

            ImGui.Text($"Status: {(component.IsPlaying ? "Playing" : "Stopped")}");

            DrawEffectsSection(component);
        });
    }

    private static void DrawEffectsSection(AudioSourceComponent component)
    {
        LayoutDrawer.DrawSeparatorWithSpacing();

        if (!ImGui.CollapsingHeader("Effects"))
            return;

        // Add effect button
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
            if (component.Effects.Any(e => e.Type == type))
                continue;

            if (ImGui.Selectable(type.ToString()))
            {
                component.Effects.Add(new AudioEffectConfig
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