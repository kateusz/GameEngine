using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class SubTextureRendererComponentEditor : IComponentEditor
{
    private readonly IAssetsManager _assetsManager;

    public SubTextureRendererComponentEditor(IAssetsManager assetsManager)
    {
        _assetsManager = assetsManager;
    }

    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<SubTextureRendererComponent>("Sub Texture Renderer", e, entity =>
        {
            var component = entity.GetComponent<SubTextureRendererComponent>();

            // Disable all fields if entity has AnimationComponent (AnimationSystem controls these values)
            var hasAnimationComponent = entity.HasComponent<AnimationComponent>();
            ImGui.BeginDisabled(hasAnimationComponent);

            TextureDropTarget.Draw("Texture", texture => component.Texture = texture, _assetsManager);

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