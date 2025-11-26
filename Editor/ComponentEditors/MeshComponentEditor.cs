using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine;
using Engine.Renderer;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class MeshComponentEditor : IComponentEditor
{
    private readonly IAssetsManager _assetsManager;

    public MeshComponentEditor(IAssetsManager assetsManager)
    {
        _assetsManager = assetsManager;
    }

    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<MeshComponent>("Mesh", e, entity =>
        {
            var meshComponent = entity.GetComponent<MeshComponent>();

            ButtonDrawer.DrawButton("Load OBJ", 100, 0, () =>
            {
                string objPath = "assets/objModels/person.model";
                if (File.Exists(objPath))
                {
                    var mesh = MeshFactory.Create(objPath);
                    mesh.Initialize();
                    meshComponent.SetMesh(mesh);
                }
            });

            ImGui.Text($"Mesh: {meshComponent.Mesh.Name}");
            ImGui.Text($"Vertices: {meshComponent.Mesh.Vertices.Count}");
            ImGui.Text($"Indices: {meshComponent.Mesh.Indices.Count}");

            MeshDropTarget.Draw(meshComponent, _assetsManager);
        });
    }
}