using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using ImGuiNET;
using SceneComponents.Rendering;
using Serilog;

namespace Editor.ComponentEditors;

public class MeshComponentEditor(
    ModelSceneImporter modelSceneImporter,
    ISceneContext sceneContext)
    : IComponentEditor
{
    private static readonly ILogger Logger = Log.ForContext(typeof(MeshComponentEditor));
    
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<MeshComponent>("Mesh", entity, () =>
        {
            var meshComponent = entity.GetComponent<MeshComponent>();

            ButtonDrawer.DrawButton("Load Cube", EditorUIConstants.DefaultButtonWidth, 0, () =>
            {
                meshComponent.UseBuiltinCube = true;
                meshComponent.ModelPath = null;
                meshComponent.MeshIndex = null;
            });

            ImGui.SameLine();
            ButtonDrawer.DrawButton("Drop Mesh", EditorUIConstants.DefaultButtonWidth, 0);
            _ = MeshDropTarget.Draw(modelSceneImporter, sceneContext, Logger);

            if (meshComponent.UseBuiltinCube)
            {
                ImGui.Text("Mesh: Built-in Cube");
            }
            else if (!string.IsNullOrWhiteSpace(meshComponent.ModelPath))
            {
                ImGui.Text($"Model: {meshComponent.ModelPath}");
                if (meshComponent.MeshIndex.HasValue)
                    ImGui.Text($"Mesh Index: {meshComponent.MeshIndex.Value}");
            }
            else
            {
                ImGui.Text("Mesh: None");
            }
        });
    }
}
