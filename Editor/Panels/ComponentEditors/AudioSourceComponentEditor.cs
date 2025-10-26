using ECS;
using Editor.Panels.Elements;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

public class AudioSourceComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<AudioSourceComponent>("Audio Source", e, entity =>
        {
            var component = entity.GetComponent<AudioSourceComponent>();

            // Audio clip (for now, just display the path or null)
            ImGui.Text("Audio Clip:");
            ImGui.SameLine();
            if (component.AudioClip != null)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.5f, 1.0f, 0.5f, 1.0f),
                    System.IO.Path.GetFileName(component.AudioClip.Path));
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.5f, 0.5f, 1.0f), "None");
            }

            // Volume slider
            UIPropertyRenderer.DrawPropertyField("Volume", component.Volume,
                newValue => component.Volume = System.Math.Clamp((float)newValue, 0.0f, 1.0f));

            // Pitch slider
            UIPropertyRenderer.DrawPropertyField("Pitch", component.Pitch,
                newValue => component.Pitch = System.Math.Clamp((float)newValue, 0.1f, 3.0f));

            // Loop checkbox
            UIPropertyRenderer.DrawPropertyField("Loop", component.Loop,
                newValue => component.Loop = (bool)newValue);

            // Play on awake checkbox
            UIPropertyRenderer.DrawPropertyField("Play On Awake", component.PlayOnAwake,
                newValue => component.PlayOnAwake = (bool)newValue);

            // 3D audio checkbox
            UIPropertyRenderer.DrawPropertyField("Is 3D", component.Is3D,
                newValue => component.Is3D = (bool)newValue);

            // 3D audio settings (only show if Is3D is true)
            if (component.Is3D)
            {
                ImGui.Indent();

                UIPropertyRenderer.DrawPropertyField("Min Distance", component.MinDistance,
                    newValue => component.MinDistance = System.Math.Max((float)newValue, 0.1f));

                UIPropertyRenderer.DrawPropertyField("Max Distance", component.MaxDistance,
                    newValue => component.MaxDistance = System.Math.Max((float)newValue, component.MinDistance));

                ImGui.Unindent();
            }

            // Playback controls
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("Playback Controls:");

            if (ImGui.Button("Play"))
            {
                component.Play();
            }
            ImGui.SameLine();
            if (ImGui.Button("Pause"))
            {
                component.Pause();
            }
            ImGui.SameLine();
            if (ImGui.Button("Stop"))
            {
                component.Stop();
            }

            // Playing status
            ImGui.Text($"Status: {(component.IsPlaying ? "Playing" : "Stopped")}");
        });
    }
}
