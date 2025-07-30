using System.Numerics;
using ECS;
using Editor.Panels.Elements;
using Engine.Renderer.Models;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

public class MeshComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<MeshComponent>("Mesh", e, entity =>
        {
            var meshComponent = entity.GetComponent<MeshComponent>();
            
            if (ImGui.Button("Load OBJ", new Vector2(100.0f, 0.0f)))
            {
                string objPath = "assets/objModels/person.model";
                if (File.Exists(objPath))
                {
                    var mesh = MeshFactory.Create(objPath);
                    mesh.Initialize();
                    meshComponent.SetMesh(mesh);
                }
            }

            ImGui.Text($"Mesh: {meshComponent.Mesh.Name}");
            ImGui.Text($"Vertices: {meshComponent.Mesh.Vertices.Count}");
            ImGui.Text($"Indices: {meshComponent.Mesh.Indices.Count}");

            MeshDropTarget.Draw(meshComponent);
        });
    }
}