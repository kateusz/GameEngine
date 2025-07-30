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
            
            var offset = component.Offset;
            UIPropertyRenderer.DrawPropertyRow("Offset", () => ImGui.DragFloat2("##Offset", ref offset));
            if (component.Offset != offset)
                component.Offset = offset;
                
            var size = component.Size;
            UIPropertyRenderer.DrawPropertyRow("Size", () => ImGui.DragFloat2("##Size", ref size));
            if (component.Size != size)
                component.Size = size;
                
            float density = component.Density;
            UIPropertyRenderer.DrawPropertyRow("Density", () => ImGui.DragFloat("##Density", ref density, 0.1f, 0.0f, 1.0f));
            if (component.Density != density)
                component.Density = density;
                
            float friction = component.Friction;
            UIPropertyRenderer.DrawPropertyRow("Friction", () => ImGui.DragFloat("##Friction", ref friction, 0.1f, 0.0f, 1.0f));
            if (component.Friction != friction)
                component.Friction = friction;
                
            float restitution = component.Restitution;
            UIPropertyRenderer.DrawPropertyRow("Restitution", () => ImGui.DragFloat("##Restitution", ref restitution, 0.1f, 0.0f, 1.0f));
            if (component.Restitution != restitution)
                component.Restitution = restitution;
        });
    }
}