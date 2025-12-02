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
            audioDropTarget.Draw("Audio Clip", component.AudioClip, audioClip => { component.AudioClip = audioClip; });
            
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
        });
    }
}