using System.Numerics;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Features.Viewport;
using Editor.Publisher;
using ImGuiNET;

namespace Editor.Features.Shell;

public class EditorDockspace(
    EditorMenuBar menuBar,
    EditorPanels panels,
    IEditorViewport editorViewport,
    ViewportComponents viewport,
    EditorSettingsUI editorSettingsUI,
    NewProjectPopup newProjectPopup,
    SceneSettingsPopup sceneSettingsPopup,
    PublishSettingsUI publishSettingsUI)
{
    public void Draw(TimeSpan deltaTime)
    {
        var dockspaceOpen = true;
        const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;

        var viewPort = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewPort.Pos);
        ImGui.SetNextWindowSize(viewPort.Size);
        ImGui.SetNextWindowViewport(viewPort.ID);

        ImGui.Begin("DockSpace Demo", ref dockspaceOpen, windowFlags);
        {
            var dockspaceId = ImGui.GetID("MyDockSpace");
            ImGui.DockSpace(dockspaceId, new Vector2(0.0f, 0.0f), ImGuiDockNodeFlags.None);

            menuBar.Render();
            panels.Draw(editorViewport.HoveredEntity, editorViewport.Camera, deltaTime);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            editorViewport.LayoutAndRender(deltaTime);
            ImGui.PopStyleVar();

            viewport.SceneToolbar.Render();
            ImGui.End();
        }

        editorSettingsUI.Render();
        newProjectPopup.Render();
        sceneSettingsPopup.Render();
        publishSettingsUI.Render();
    }
}
