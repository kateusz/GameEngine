using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Project;
using Engine.Renderer.Models;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class SkinnedAnimationComponentEditor(
    UIPropertyRenderer propertyRenderer,
    IEditorHistory history,
    IModelFactory models) : ComponentEditor<SkinnedAnimationComponent>(history)
{
    protected override string DisplayName => "Skinned Animation";

    protected override void DrawContent(SkinnedAnimationComponent component, Entity entity)
    {
        var options = new List<string> { "Rest" };
        if (entity.TryGetComponent<ModelRendererComponent>(out var renderer)
            && !string.IsNullOrWhiteSpace(renderer.ModelPath))
        {
            var model = models.Create(PathBuilder.Resolve(renderer.ModelPath));
            if (model?.Skin != null)
            {
                foreach (var clip in model.Skin.Clips)
                {
                    if (!options.Contains(clip.Name))
                        options.Add(clip.Name);
                }
            }
        }

        if (!string.IsNullOrEmpty(component.ClipName) && !options.Contains(component.ClipName))
            options.Add(component.ClipName);

        var current = string.IsNullOrEmpty(component.ClipName) ? "Rest" : component.ClipName;
        LayoutDrawer.DrawComboBox("Clip", current, options.ToArray(), selected =>
        {
            var name = selected == "Rest" ? "" : selected;
            MultiField.WriteEach(entity, (Entity target, string value) =>
                target.GetComponent<SkinnedAnimationComponent>().ClipName = value, name);
        });

        propertyRenderer.DrawPropertyField("Loop", entity,
            e => e.GetComponent<SkinnedAnimationComponent>().Loop,
            (e, value) => e.GetComponent<SkinnedAnimationComponent>().Loop = value);
    }
}
