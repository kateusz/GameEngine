using ECS;
using Editor.Panels.Elements;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

public class BoxCollider2DComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<BoxCollider2DComponent>("Box Collider 2D", e, entity =>
        {
            var component = entity.GetComponent<BoxCollider2DComponent>();

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