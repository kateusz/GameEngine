using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using SceneComponents.Audio;

namespace Editor.ComponentEditors;

public class AudioListenerComponentEditor(UIPropertyRenderer propertyRenderer) : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<AudioListenerComponent>("Audio Listener", entity, () =>
        {
            var component = entity.GetComponent<AudioListenerComponent>();

            propertyRenderer.DrawPropertyField("Is Active", component.IsActive,
                newValue => component.IsActive = (bool)newValue);
        });
    }
}
