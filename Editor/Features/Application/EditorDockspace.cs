using System.Numerics;
using Editor.Features.Import;
using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Features.Viewport;
using Editor.Publisher;
using Engine.Scene;
using ImGuiNET;
using Serilog;

namespace Editor.Features.Application;

public class EditorDockspace(
    EditorMenuBar menuBar,
    EditorPanels panels,
    IEditorViewport editorViewport,
    ViewportComponents viewport,
    EditorSettingsUI editorSettingsUI,
    NewProjectPopup newProjectPopup,
    Import3DModelPopup import3DModelPopup,
    SceneSettingsPopup sceneSettingsPopup,
    PublishSettingsUI publishSettingsUI,
    ISceneManager sceneManager,
    IEditorPreferences editorPreferences,
    ISceneContext sceneContext)
{
    private static readonly ILogger Logger = Log.ForContext<EditorDockspace>();
    private const int MinAutosaveIntervalSeconds = 5;

    private TimeSpan _timeSinceAutosave;

    public void Draw(TimeSpan deltaTime)
    {
        TickAutosave(deltaTime);

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
        import3DModelPopup.Render();
        sceneSettingsPopup.Render();
        publishSettingsUI.Render();
    }

    private void TickAutosave(TimeSpan deltaTime)
    {
        var intervalSeconds = editorPreferences.AutosaveIntervalSeconds;
        if (intervalSeconds <= 0
            || sceneContext.State != SceneState.Edit
            || sceneContext.ActiveScene is null
            || string.IsNullOrEmpty(sceneManager.GetCurrentScenePath()))
            return;

        _timeSinceAutosave += deltaTime;
        var interval = TimeSpan.FromSeconds(intervalSeconds < MinAutosaveIntervalSeconds
            ? MinAutosaveIntervalSeconds
            : intervalSeconds);
        if (_timeSinceAutosave < interval)
            return;

        _timeSinceAutosave = TimeSpan.Zero;
        try
        {
            sceneManager.Save(compileScripts: false);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Scene autosave failed");
        }
    }
}
