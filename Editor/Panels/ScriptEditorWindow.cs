using System.Numerics;
using Engine.Scripting;
using ImGuiNET;
using NLog;

namespace Editor.Panels;

public class ScriptEditorWindow
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        
    private bool _isOpen = false;
    private string _scriptName = string.Empty;
    private string _scriptContent = string.Empty;
    private string _originalScriptContent = string.Empty;
    private bool _isNewScript = false;
    private bool _hasChanges = false;
    private string _errorMessage = string.Empty;
    private bool _showSaveConfirmation = false;
    private bool _showCloseConfirmation = false;
    private Action<bool> _onCloseCallback;
        
    // Editor settings
    private readonly Vector2 _minWindowSize = new Vector2(800, 600);
    private readonly float _lineNumbersWidth = 50.0f;
    private readonly Vector4 _lineNumbersColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
    private readonly Vector4 _errorColor = new Vector4(1.0f, 0.3f, 0.3f, 1.0f);
        
    public void Open(string scriptName, bool isNewScript = false, Action<bool> onCloseCallback = null)
    {
        _scriptName = scriptName;
        _isNewScript = isNewScript;
        _isOpen = true;
        _errorMessage = string.Empty;
        _onCloseCallback = onCloseCallback;
            
        // Load script content
        if (isNewScript)
        {
            _scriptContent = ScriptEngine.GenerateScriptTemplate(scriptName);
            _originalScriptContent = _scriptContent;
        }
        else
        {
            _scriptContent = ScriptEngine.Instance.GetScriptSource(scriptName);
            _originalScriptContent = _scriptContent;
        }
            
        _hasChanges = false;
    }
        
    public void Close(bool success = false)
    {
        _isOpen = false;
        _scriptName = string.Empty;
        _scriptContent = string.Empty;
        _originalScriptContent = string.Empty;
        _errorMessage = string.Empty;
        _hasChanges = false;
        _showCloseConfirmation = false;
        _showSaveConfirmation = false;
            
        _onCloseCallback?.Invoke(success);
    }
        
    public void Render()
    {
        if (!_isOpen) return;
            
        // Center the window
        var viewport = ImGui.GetMainViewport();
        var center = viewport.GetCenter();
        
        ImGui.SetNextWindowFocus();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(_minWindowSize, ImGuiCond.Appearing);
            
        // Configure window flags
        var windowFlags = ImGuiWindowFlags.Modal | 
                          ImGuiWindowFlags.NoDocking | 
                          ImGuiWindowFlags.NoCollapse;
                            
        string title = _isNewScript ? $"New Script: {_scriptName}" : $"Edit Script: {_scriptName}";
        if (_hasChanges) title += "*";
            
        if (ImGui.Begin(title, ref _isOpen, windowFlags))
        {
            // Toolbar
            RenderToolbar();
                
            // Script editor
            RenderEditor();
                
            // Status bar
            RenderStatusBar();
                
            // Error message
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, _errorColor);
                ImGui.TextWrapped(_errorMessage);
                ImGui.PopStyleColor();
            }
        }
        ImGui.End();
            
        // Handle close requests
        if (!_isOpen)
        {
            if (_hasChanges && !_showCloseConfirmation)
            {
                // Only show confirmation if user tried to close with unsaved changes
                _showCloseConfirmation = true;
                _isOpen = true; // Reopen until user confirms
            }
            else
            {
                Close();
            }
        }
            
        // Handle confirmation dialogs
        RenderConfirmationDialogs();
    }
        
    private void RenderToolbar()
    {
        if (ImGui.Button("Save", new Vector2(80, 0)))
        {
            SaveScript();
        }
            
        ImGui.SameLine();
        if (ImGui.Button("Save and Close", new Vector2(120, 0)))
        {
            SaveAndClose();
        }
            
        ImGui.SameLine();
        if (ImGui.Button("Close", new Vector2(80, 0)))
        {
            if (_hasChanges)
            {
                _showCloseConfirmation = true;
            }
            else
            {
                Close();
            }
        }
            
        ImGui.Separator();
    }
        
    private void RenderEditor()
    {
        var windowSize = ImGui.GetContentRegionAvail();
            
        // Create a child window for the text editor
        ImGui.BeginChild("ScriptEditorChild", new Vector2(windowSize.X, windowSize.Y - 25));
            
        var textFlags = ImGuiInputTextFlags.AllowTabInput | 
                        ImGuiInputTextFlags.CtrlEnterForNewLine;
            
        var isFocused = ImGui.IsWindowFocused();
            
        // Input text with multiline
        if (ImGui.InputTextMultiline("##ScriptContent", ref _scriptContent, 
                1024 * 1024, new Vector2(-1, -1), textFlags))
        {
            _hasChanges = _scriptContent != _originalScriptContent;
        }
            
        // Auto-focus the text editor when the window opens
        if (_isNewScript && isFocused)
        {
            ImGui.SetKeyboardFocusHere(-1);
            _isNewScript = false;
        }
            
        ImGui.EndChild();
    }
        
    private void RenderStatusBar()
    {
        ImGui.BeginChild("StatusBar", new Vector2(-1, 20));
            
        ImGui.Text(_hasChanges ? "Modified" : "Saved");
            
        ImGui.SameLine(ImGui.GetWindowWidth() - 150);
        ImGui.Text($"Characters: {_scriptContent.Length}");
            
        ImGui.EndChild();
    }
        
    private void RenderConfirmationDialogs()
    {
        // Unsaved changes confirmation
        if (_showCloseConfirmation)
        {
            ImGui.OpenPopup("Unsaved Changes");
        }
            
        if (ImGui.BeginPopupModal("Unsaved Changes", ref _showCloseConfirmation, 
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("You have unsaved changes. What would you like to do?");
            ImGui.Separator();
                
            if (ImGui.Button("Save and Close", new Vector2(120, 0)))
            {
                _showCloseConfirmation = false;
                SaveAndClose();
            }
                
            ImGui.SameLine();
            if (ImGui.Button("Discard Changes", new Vector2(120, 0)))
            {
                _showCloseConfirmation = false;
                Close();
            }
                
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showCloseConfirmation = false;
            }
                
            ImGui.EndPopup();
        }
            
        // Save confirmation
        if (_showSaveConfirmation)
        {
            ImGui.OpenPopup("Save Successful");
        }
            
        if (ImGui.BeginPopupModal("Save Successful", ref _showSaveConfirmation, 
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Script saved successfully!");
            ImGui.Separator();
                
            if (ImGui.Button("Close", new Vector2(120, 0)))
            {
                _showSaveConfirmation = false;
                Close(true);
            }
                
            ImGui.SameLine();
            if (ImGui.Button("Continue Editing", new Vector2(120, 0)))
            {
                _showSaveConfirmation = false;
            }
                
            ImGui.EndPopup();
        }
    }
        
    private async void SaveScript()
    {
        try
        {
            var (success, errors) = await ScriptEngine.Instance.CreateOrUpdateScriptAsync(_scriptName, _scriptContent);
                
            if (success)
            {
                _originalScriptContent = _scriptContent;
                _hasChanges = false;
                _errorMessage = string.Empty;
                Logger.Info($"Script '{_scriptName}' saved successfully");
            }
            else
            {
                _errorMessage = $"Compilation failed: {string.Join("\n", errors)}";
                Logger.Error(_errorMessage);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error saving script: {ex.Message}";
            Logger.Error(ex, "Error saving script");
        }
    }
        
    private async void SaveAndClose()
    {
        try
        {
            var (success, errors) = await ScriptEngine.Instance.CreateOrUpdateScriptAsync(_scriptName, _scriptContent);
            if (success)
            {
                _originalScriptContent = _scriptContent;
                _hasChanges = false;
                _errorMessage = string.Empty;
                // Close immediately after save
                Close(true);
            }
            else
            {
                _errorMessage = $"Compilation failed: {string.Join("\n", errors)}";
                Logger.Error(_errorMessage);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error saving script: {ex.Message}";
            Logger.Error(ex, "Error saving script");
        }
    }
}