using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using ImGuiNET;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class AnimatorComponentEditor(UIPropertyRenderer propertyRenderer)
    : ComponentEditor<AnimatorComponent>
{
    protected override string DisplayName => "Animator";

    protected override void DrawContent(AnimatorComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Clip Name", component.ClipName ?? string.Empty,
            newValue => component.ClipName = (string)newValue);
        propertyRenderer.DrawPropertyField("Time", component.Time,
            newValue => component.Time = (float)newValue);
        propertyRenderer.DrawPropertyField("Playing", component.IsPlaying,
            newValue => component.IsPlaying = (bool)newValue);
        propertyRenderer.DrawPropertyField("Loop", component.Loop,
            newValue => component.Loop = (bool)newValue);
        propertyRenderer.DrawPropertyField("Speed", component.Speed,
            newValue => component.Speed = (float)newValue);
        propertyRenderer.DrawPropertyField("Apply Root Motion", component.ApplyRootMotion,
            newValue => component.ApplyRootMotion = (bool)newValue);

        UIPropertyRenderer.DrawPropertyRow("Playback", () =>
        {
            if (ImGui.Button("Play##Animator"))
            {
                if (!string.IsNullOrWhiteSpace(component.ClipName))
                    component.Play(component.ClipName);
                else
                    component.IsPlaying = true;
            }

            ImGui.SameLine();
            if (ImGui.Button("Pause##Animator"))
                component.Pause();
            ImGui.SameLine();
            if (ImGui.Button("Stop##Animator"))
                component.Stop();
        });
    }
}
