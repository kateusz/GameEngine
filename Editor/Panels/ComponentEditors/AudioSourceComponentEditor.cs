using ECS;
using Editor.Panels.Elements;
using Engine.Audio;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

public class AudioSourceComponentEditor : IComponentEditor
{
    private readonly IAudioEngine _audioEngine;
    private readonly AudioDropTarget _audioDropTarget;

    public AudioSourceComponentEditor(IAudioEngine audioEngine, AudioDropTarget audioDropTarget)
    {
        _audioEngine = audioEngine ?? throw new ArgumentNullException(nameof(audioEngine));
        _audioDropTarget = audioDropTarget ?? throw new ArgumentNullException(nameof(audioDropTarget));
    }

    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<AudioSourceComponent>("Audio Source", e, entity =>
        {
            var component = entity.GetComponent<AudioSourceComponent>();

            // Audio clip with drag-and-drop support
            _audioDropTarget.Draw("Audio Clip", component.AudioClip, audioClip =>
            {
                component.AudioClip = audioClip;
            });

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
                var path = entity.GetComponent<AudioSourceComponent>().AudioClip?.Path;
                if(!string.IsNullOrWhiteSpace(path))
                    _audioEngine.PlayOneShot(path, volume: 0.5f);
            }

            // Playing status
            ImGui.Text($"Status: {(component.IsPlaying ? "Playing" : "Stopped")}");
        });
    }
}
