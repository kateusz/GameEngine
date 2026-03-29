using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class MeshComponentEditor(
    IAssetsManager assetsManager,
    IMeshFactory meshFactory)
    : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<MeshComponent>("Mesh", entity, () =>
        {
            var meshComponent = entity.GetComponent<MeshComponent>();

            ButtonDrawer.DrawButton("Load Cube", EditorUIConstants.DefaultButtonWidth, 0, () =>
            {
                var cube = meshFactory.CreateCube();
                meshComponent.SetModel([cube]);
            });

            if (meshComponent.MeshCount > 0)
            {
                ImGui.Text($"Meshes: {meshComponent.MeshCount}");
                foreach (var mesh in meshComponent.Meshes)
                {
                    ImGui.Text($"  {mesh.Name}: {mesh.Vertices.Count} verts, {mesh.Indices.Count} indices");
                }
            }
            else if (!string.IsNullOrWhiteSpace(meshComponent.ModelPath))
            {
                ImGui.Text($"Model: {meshComponent.ModelPath} (not loaded)");
            }
            else
            {
                ImGui.Text("Mesh: None");
            }

            MeshDropTarget.Draw(meshComponent, assetsManager, meshFactory);
        });
    }
}
