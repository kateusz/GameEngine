using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using SceneComponents.Audio;

namespace Editor.ComponentEditors.Audio;

public class AudioListenerComponentEditor(UIPropertyRenderer propertyRenderer)
    : ComponentEditor<AudioListenerComponent>
{
    protected override string DisplayName => "Audio Listener";

    protected override void DrawContent(AudioListenerComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Is Active", component.IsActive,
            newValue => component.IsActive = (bool)newValue);
    }
}
