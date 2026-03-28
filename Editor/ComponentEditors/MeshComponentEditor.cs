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
                meshComponent.SetMesh(cube);
            });

            if (meshComponent.Mesh != null)
            {
                ImGui.Text($"Mesh: {meshComponent.Mesh.Name}");
                ImGui.Text($"Vertices: {meshComponent.Mesh.Vertices.Count}");
                ImGui.Text($"Indices: {meshComponent.Mesh.Indices.Count}");
            }
            else if (!string.IsNullOrWhiteSpace(meshComponent.MeshPath))
            {
                ImGui.Text($"Mesh: {meshComponent.MeshPath} (not loaded)");
            }
            else
            {
                ImGui.Text("Mesh: None");
            }

            MeshDropTarget.Draw(meshComponent, assetsManager, meshFactory);
        });
    }
}
