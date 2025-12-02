using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Scene.Components;

namespace Editor.ComponentEditors;

public class AudioListenerComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<AudioListenerComponent>("Audio Listener", e, () =>
        {
            var component = e.GetComponent<AudioListenerComponent>();

            UIPropertyRenderer.DrawPropertyField("Is Active", component.IsActive,
                newValue => component.IsActive = (bool)newValue);
        });
    }
}
