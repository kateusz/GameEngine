using System.Numerics;
using Editor.Features.Viewport;
using Editor.Panels;
using ImGuiNET;

namespace Editor.Features.Application;

/// <summary>
/// Godot-style bottom panel under the center column only (left/right docks keep full height).
/// Thin tab bar always visible; click expands, click again collapses.
/// </summary>
public class BottomPanelHost(
    ConsolePanel consolePanel,
    ContentBrowserPanel contentBrowserPanel,
    IEditorViewport editorViewport)
{
    private enum Tab
    {
        None,
        Console,
        ContentBrowser
    }

    private const float TabBarHeight = 26f;
    private const float ExpandedHeight = 200f;

    private Tab _active = Tab.None;

    public void Draw()
    {
        if (!editorViewport.HasWindowRect)
            return;

        var main = ImGui.GetMainViewport();
        var height = TabBarHeight + (_active != Tab.None ? ExpandedHeight : 0f);
        // Center column only — match Viewport dock window X/width (Godot layout).
        var x = editorViewport.WindowPos.X;
        var width = editorViewport.WindowSize.X;
        var y = main.WorkPos.Y + main.WorkSize.Y - height;

        ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
        ImGui.SetNextWindowViewport(main.ID);

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoDocking
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoNavFocus
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6f, 2f));
        ImGui.Begin("##BottomPanel", flags);
        ImGui.SetWindowSize(new Vector2(width, height));

        if (_active != Tab.None)
        {
            ImGui.BeginChild("##BottomContent", new Vector2(0f, -TabBarHeight), ImGuiChildFlags.None);
            switch (_active)
            {
                case Tab.Console:
                    consolePanel.DrawContent();
                    break;
                case Tab.ContentBrowser:
                    contentBrowserPanel.DrawContent();
                    break;
            }

            ImGui.EndChild();
        }

        DrawTab("Console", Tab.Console);
        ImGui.SameLine();
        DrawTab("Content Browser", Tab.ContentBrowser);

        ImGui.End();
        ImGui.PopStyleVar(3);
    }

    private void DrawTab(string label, Tab tab)
    {
        var isActive = _active == tab;
        if (isActive)
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);

        if (ImGui.Button(label, new Vector2(0f, TabBarHeight - 4f)))
            _active = isActive ? Tab.None : tab;

        if (isActive)
            ImGui.PopStyleColor();
    }
}
