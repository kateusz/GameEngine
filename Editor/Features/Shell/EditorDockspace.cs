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
        const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking
            | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        var viewPort = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewPort.Pos);
        ImGui.SetNextWindowSize(viewPort.Size);
        ImGui.SetNextWindowViewport(viewPort.ID);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        ImGui.Begin("Editor", ref dockspaceOpen, windowFlags);
        ImGui.PopStyleVar(3);
        {
            // Fixed id — must match DockSpace ID in imgui.ini (GetID changes when window name changes).
            const uint dockspaceId = 0x3BC79352;
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
