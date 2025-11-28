using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.VertexArray;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class MeshComponentEditor : IComponentEditor
{
    private readonly IAssetsManager _assetsManager;
    private readonly IMeshFactory _meshFactory;
    private readonly IVertexArrayFactory _vertexArrayFactory;
    private readonly IVertexBufferFactory _vertexBufferFactory;
    private readonly IIndexBufferFactory _indexBufferFactory;

    public MeshComponentEditor(IAssetsManager assetsManager, IMeshFactory meshFactory,
        IVertexArrayFactory vertexArrayFactory, IVertexBufferFactory vertexBufferFactory, IIndexBufferFactory indexBufferFactory)
    {
        _assetsManager = assetsManager;
        _meshFactory = meshFactory;
        _vertexArrayFactory = vertexArrayFactory;
        _vertexBufferFactory = vertexBufferFactory;
        _indexBufferFactory = indexBufferFactory;
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
                    var mesh = _meshFactory.Create(objPath);
                    mesh.Initialize(_vertexArrayFactory, _vertexBufferFactory, _indexBufferFactory);
                    meshComponent.SetMesh(mesh);
                }
            });

            ImGui.Text($"Mesh: {meshComponent.Mesh.Name}");
            ImGui.Text($"Vertices: {meshComponent.Mesh.Vertices.Count}");
            ImGui.Text($"Indices: {meshComponent.Mesh.Indices.Count}");

            MeshDropTarget.Draw(meshComponent, _assetsManager, _meshFactory, _vertexArrayFactory, _vertexBufferFactory, _indexBufferFactory);
        });
    }
}