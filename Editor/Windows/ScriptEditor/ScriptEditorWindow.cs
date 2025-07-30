using System.Numerics;
using Editor.Panels.Elements.ScriptEditor;
using ImGuiNET;
using NLog;

namespace Editor.Windows.ScriptEditor;

public class ScriptEditorWindow
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
    private readonly ScriptEditorState _state;
    private readonly ScriptFileManager _fileManager;
    private readonly ScriptEditorToolbar _toolbar;
    private readonly ScriptEditorTextArea _textArea;
    private readonly ScriptEditorStatusBar _statusBar;
    
    private readonly Vector2 _minWindowSize = new Vector2(800, 600);

    public ScriptEditorWindow()
    {
        _state = new ScriptEditorState();
        _fileManager = new ScriptFileManager();
        _toolbar = new ScriptEditorToolbar();
        _textArea = new ScriptEditorTextArea();
        _statusBar = new ScriptEditorStatusBar();
        
        // Wire up events
        _toolbar.SaveRequested += HandleSave;
        _toolbar.SaveAndCloseRequested += HandleSaveAndClose;
        _toolbar.CloseRequested += HandleClose;
        _textArea.ContentChanged += HandleContentChanged;
    }

    public void Open(string scriptName, bool isNewScript = false, Action<bool> onCloseCallback = null)
    {
        _state.Open(scriptName, isNewScript, onCloseCallback);
        _fileManager.LoadScript(_state);
    }

    public void Close(bool success = false)
    {
        Console.WriteLine($"Close called with success: {success}"); // Debug
        _state.Close();
        _state.OnCloseCallback?.Invoke(success);
    }

    public void Render()
    {
        if (!_state.IsOpen) return;

        SetupWindow();

        var isOpen = _state.IsOpen;
        if (ImGui.Begin(_state.WindowTitle, ref isOpen, GetWindowFlags()))
        {
            _toolbar.Render(_state);
            _textArea.Render(_state);
            _statusBar.Render(_state);
            RenderErrorMessage();
        }
        ImGui.End();

        // Handle window close button (X) - only when not showing confirmation
        if (!_state.IsOpen)
        {
            HandleCloseRequest();
        }
    }

    private void SetupWindow()
    {
        var viewport = ImGui.GetMainViewport();
        var center = viewport.GetCenter();
        
        ImGui.SetNextWindowFocus();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(_minWindowSize, ImGuiCond.Appearing);
    }

    private ImGuiWindowFlags GetWindowFlags()
    {
        // Remove Modal flag to avoid conflicts with confirmation dialogs
        return ImGuiWindowFlags.NoDocking | 
               ImGuiWindowFlags.NoCollapse;
    }

    private void RenderErrorMessage()
    {
        if (!string.IsNullOrEmpty(_state.ErrorMessage))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.3f, 0.3f, 1.0f));
            ImGui.TextWrapped(_state.ErrorMessage);
            ImGui.PopStyleColor();
        }
    }

    private void HandleCloseRequest()
    {
        if (_state.HasChanges)
        {
            _state.IsOpen = true; // Keep window open while showing confirmation
        }
        else
        {
            Close();
        }
    }

    private async void HandleSave()
    {
        Console.WriteLine("HandleSave called"); // Debug
        await _fileManager.SaveScript(_state);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void HandleSaveAndClose()
    {
        Console.WriteLine("HandleSaveAndClose called"); // Debug
        var success = await _fileManager.SaveScript(_state);
        if (success)
        {
            Console.WriteLine("Save successful, closing window"); // Debug
            Close(true);
        }
        else
        {
            Console.WriteLine("Save failed, keeping window open"); // Debug
        }
    }

    private void HandleClose()
    {
        Console.WriteLine($"HandleClose called, HasChanges: {_state.HasChanges}"); // Debug
        Console.WriteLine(_state.HasChanges ? "Showing close confirmation dialog" : "No changes, closing directly"); // Debug
        Close();
    }

    private void HandleContentChanged(string newContent)
    {
        _state.UpdateContent(newContent);
    }
}