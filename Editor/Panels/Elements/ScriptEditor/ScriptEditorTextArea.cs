using System.Numerics;
using Editor.Windows.ScriptEditor;
using ImGuiNET;

namespace Editor.Panels.Elements.ScriptEditor;

public class ScriptEditorTextArea
{
    public event Action<string> ContentChanged;

    public void Render(ScriptEditorState state)
    {
        var windowSize = ImGui.GetContentRegionAvail();
        ImGui.BeginChild("ScriptEditorChild", new Vector2(windowSize.X, windowSize.Y - 25));

        HandleKeyboardShortcuts(state);
        RenderTextEditor(state);
        RenderContextMenu(state);

        ImGui.EndChild();
    }

    private void HandleKeyboardShortcuts(ScriptEditorState state)
    {
        var isFocused = ImGui.IsWindowFocused();
        var io = ImGui.GetIO();

        if (isFocused && io.KeyCtrl)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.C))
            {
                ImGui.SetClipboardText(state.ScriptContent);
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.V))
            {
                var clipboard = ImGui.GetClipboardText();
                if (!string.IsNullOrEmpty(clipboard))
                {
                    ContentChanged?.Invoke(state.ScriptContent + clipboard);
                }
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.X))
            {
                ImGui.SetClipboardText(state.ScriptContent);
                ContentChanged?.Invoke(string.Empty);
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.A))
            {
                // Select all - handled by ImGui internally, but we can add visual feedback
            }
        }
    }

    private void RenderTextEditor(ScriptEditorState state)
    {
        var textFlags = ImGuiInputTextFlags.AllowTabInput |
                        ImGuiInputTextFlags.CtrlEnterForNewLine;

        var content = state.ScriptContent;
        if (ImGui.InputTextMultiline("##ScriptContent", ref content,
                1024 * 1024, new Vector2(-1, -1), textFlags))
        {
            ContentChanged?.Invoke(content);
        }

        // Auto-focus when opening new script
        if (state.IsNewScript && ImGui.IsWindowFocused())
        {
            ImGui.SetKeyboardFocusHere(-1);
            state.MarkAsOpened(); // Mark as opened so we don't keep focusing
        }
    }

    private void RenderContextMenu(ScriptEditorState state)
    {
        if (ImGui.BeginPopupContextItem("ScriptEditorContextMenu"))
        {
            if (ImGui.MenuItem("Copy", "Ctrl+C"))
            {
                ImGui.SetClipboardText(state.ScriptContent);
            }
            
            if (ImGui.MenuItem("Paste", "Ctrl+V"))
            {
                var clipboard = ImGui.GetClipboardText();
                if (!string.IsNullOrEmpty(clipboard))
                {
                    ContentChanged?.Invoke(state.ScriptContent + clipboard);
                }
            }
            
            if (ImGui.MenuItem("Cut", "Ctrl+X"))
            {
                ImGui.SetClipboardText(state.ScriptContent);
                ContentChanged?.Invoke(string.Empty);
            }

            if (ImGui.MenuItem("Select All", "Ctrl+A"))
            {
                // This is handled by ImGui internally when Ctrl+A is pressed
                // We include it here for discoverability
            }
            
            ImGui.EndPopup();
        }
    }
}