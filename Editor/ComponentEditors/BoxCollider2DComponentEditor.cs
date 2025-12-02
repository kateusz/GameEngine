using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Scene.Components;

namespace Editor.ComponentEditors;

public class BoxCollider2DComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<BoxCollider2DComponent>("Box Collider 2D", e, () =>
        {
            var component = e.GetComponent<BoxCollider2DComponent>();

            UIPropertyRenderer.DrawPropertyField("Offset", component.Offset,
                newValue => component.Offset = (System.Numerics.Vector2)newValue);

            UIPropertyRenderer.DrawPropertyField("Size", component.Size,
                newValue => component.Size = (System.Numerics.Vector2)newValue);

            UIPropertyRenderer.DrawPropertyField("Density", component.Density,
                newValue => component.Density = (float)newValue);

            UIPropertyRenderer.DrawPropertyField("Friction", component.Friction,
                newValue => component.Friction = (float)newValue);

            UIPropertyRenderer.DrawPropertyField("Restitution", component.Restitution,
                newValue => component.Restitution = (float)newValue);
        });
    }
}