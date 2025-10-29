using ECS;
using Editor.Panels.Elements;
using Engine.Scene.Components;

namespace Editor.Panels.ComponentEditors;

public class AudioListenerComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<AudioListenerComponent>("Audio Listener", e, entity =>
        {
            var component = entity.GetComponent<AudioListenerComponent>();

            UIPropertyRenderer.DrawPropertyField("Is Active", component.IsActive,
                newValue => component.IsActive = (bool)newValue);
        });
    }
}
