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

            ImGui.SameLine();
            ButtonDrawer.DrawButton("Drop FBX", EditorUIConstants.DefaultButtonWidth, 0);
            MeshDropTarget.Draw(meshComponent, assetsManager, meshFactory);

            if (meshComponent.MeshCount > 0)
            {
                ImGui.Text($"Meshes: {meshComponent.MeshCount}");
                ImGui.BeginChild("MeshList", new System.Numerics.Vector2(0, 200), ImGuiChildFlags.Border, ImGuiWindowFlags.None);
                foreach (var mesh in meshComponent.Meshes)
                {
                    ImGui.Text($"  {mesh.Name}: {mesh.Vertices.Count} verts, {mesh.Indices.Count} indices");
                }
                ImGui.EndChild();
            }
            else if (!string.IsNullOrWhiteSpace(meshComponent.ModelPath))
            {
                ImGui.Text($"Model: {meshComponent.ModelPath} (not loaded)");
            }
            else
            {
                ImGui.Text("Mesh: None");
            }
        });
    }
}
