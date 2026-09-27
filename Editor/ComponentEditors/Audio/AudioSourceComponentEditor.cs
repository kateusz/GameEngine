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
        audioDropTarget.Draw("Audio Clip",
            relativePath => MultiField.WriteEach(entity, (Entity e, string path) =>
            {
                e.GetComponent<AudioSourceComponent>().AudioClipPath = path;
            }, relativePath),
            MultiField.UniformPath(entity, e => e.GetComponent<AudioSourceComponent>().AudioClipPath));

        propertyRenderer.DrawPropertyField("Volume", entity,
            e => e.GetComponent<AudioSourceComponent>().Volume,
            (e, v) => e.GetComponent<AudioSourceComponent>().Volume = System.Math.Clamp(v, 0.0f, 1.0f),
            MultiField.SameFloat);
        propertyRenderer.DrawPropertyField("Pitch", entity,
            e => e.GetComponent<AudioSourceComponent>().Pitch,
            (e, v) => e.GetComponent<AudioSourceComponent>().Pitch = System.Math.Clamp(v, 0.1f, 3.0f),
            MultiField.SameFloat);
        propertyRenderer.DrawPropertyField("Loop", entity,
            e => e.GetComponent<AudioSourceComponent>().Loop,
            (e, v) => e.GetComponent<AudioSourceComponent>().Loop = v);
        propertyRenderer.DrawPropertyField("Play On Awake", entity,
            e => e.GetComponent<AudioSourceComponent>().PlayOnAwake,
            (e, v) => e.GetComponent<AudioSourceComponent>().PlayOnAwake = v);
        propertyRenderer.DrawPropertyField("Is 3D", entity,
            e => e.GetComponent<AudioSourceComponent>().Is3D,
            (e, v) => e.GetComponent<AudioSourceComponent>().Is3D = v);

        if (MultiField.For(entity).All(e => e.GetComponent<AudioSourceComponent>().Is3D))
        {
            LayoutDrawer.DrawIndentedSection(() =>
            {
                propertyRenderer.DrawPropertyField("Min Distance", entity,
                    e => e.GetComponent<AudioSourceComponent>().MinDistance,
                    (e, v) => e.GetComponent<AudioSourceComponent>().MinDistance = System.Math.Max(v, 0.1f),
                    MultiField.SameFloat);

                propertyRenderer.DrawPropertyField("Max Distance", entity,
                    e => e.GetComponent<AudioSourceComponent>().MaxDistance,
                    (e, v) =>
                    {
                        var min = e.GetComponent<AudioSourceComponent>().MinDistance;
                        e.GetComponent<AudioSourceComponent>().MaxDistance = System.Math.Max(v, min);
                    },
                    MultiField.SameFloat);
            });
        }

        if (MultiField.Targets is not null)
            return;

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
