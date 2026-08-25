using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Drawers;
using SceneComponents;

namespace Editor.ComponentEditors;

public class TiledObjectComponentEditor(IEditorHistory history) : ComponentEditor<TiledObjectComponent>(history)
{
    protected override string DisplayName => "Tiled Object";

    protected override void DrawContent(TiledObjectComponent component, Entity entity)
    {
        TextDrawer.DrawInfoText($"Tiled Id: {component.TiledId}");
        TextDrawer.DrawInfoText($"Name: {component.ObjectName}");
        TextDrawer.DrawInfoText($"Type: {component.ObjectType}");
        foreach (var (key, value) in component.Properties)
            TextDrawer.DrawInfoText($"{key}: {value}");
    }
}
