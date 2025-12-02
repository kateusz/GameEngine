using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.VertexArray;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class MeshComponentEditor(
    IAssetsManager assetsManager,
    IMeshFactory meshFactory,
    IVertexArrayFactory vertexArrayFactory,
    IVertexBufferFactory vertexBufferFactory,
    IIndexBufferFactory indexBufferFactory)
    : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<MeshComponent>("Mesh", entity, () =>
        {
            var meshComponent = entity.GetComponent<MeshComponent>();

            ButtonDrawer.DrawButton("Load OBJ", 100, 0, () =>
            {
                // TODO
                const string objPath = "assets/objModels/person.model";
                if (!File.Exists(objPath)) 
                    return;
                
                var mesh = meshFactory.Create(objPath);
                mesh.Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
                meshComponent.SetMesh(mesh);
            });

            ImGui.Text($"Mesh: {meshComponent.Mesh.Name}");
            ImGui.Text($"Vertices: {meshComponent.Mesh.Vertices.Count}");
            ImGui.Text($"Indices: {meshComponent.Mesh.Indices.Count}");

            MeshDropTarget.Draw(meshComponent, assetsManager, meshFactory, vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
        });
    }
}