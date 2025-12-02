using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Scene.Components;

namespace Editor.ComponentEditors;

public class AudioListenerComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<AudioListenerComponent>("Audio Listener", entity, () =>
        {
            var component = entity.GetComponent<AudioListenerComponent>();

            UIPropertyRenderer.DrawPropertyField("Is Active", component.IsActive,
                newValue => component.IsActive = (bool)newValue);
        });
    }
}
