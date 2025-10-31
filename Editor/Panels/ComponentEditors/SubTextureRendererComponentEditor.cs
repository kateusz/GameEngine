using ECS;
using Editor.Panels.Elements;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

public class SubTextureRendererComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<SubTextureRendererComponent>("Sub Texture Renderer", e, entity =>
        {
            var component = entity.GetComponent<SubTextureRendererComponent>();

            // Disable all fields if entity has AnimationComponent (AnimationSystem controls these values)
            var hasAnimationComponent = entity.HasComponent<AnimationComponent>();
            ImGui.BeginDisabled(hasAnimationComponent);

            TextureDropTarget.Draw("Texture", texture => component.Texture = texture);

            UIPropertyRenderer.DrawPropertyField("Sub texture coords", component.Coords,
                newValue => component.Coords = (System.Numerics.Vector2)newValue);

            ImGui.Separator();
            ImGui.Text("Atlas Settings");

            UIPropertyRenderer.DrawPropertyField("Cell Size", component.CellSize,
                newValue => component.CellSize = (System.Numerics.Vector2)newValue);

            UIPropertyRenderer.DrawPropertyField("Sprite Size", component.SpriteSize,
                newValue => component.SpriteSize = (System.Numerics.Vector2)newValue);

            ImGui.EndDisabled();
        });
    }
}