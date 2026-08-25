using System.Numerics;
using Editor.Features.Application;
using Editor.Features.Scripting;
using Editor.UI.Drawers;
using ImGuiColorTextEditNet;
using ImGuiColorTextEditNet.Editor;
using ImGuiColorTextEditNet.Input;
using ImGuiColorTextEditNet.Syntax;
using ImGuiNET;
using Serilog;

namespace Editor.Panels;

public sealed class ScriptEditorPanel(GameScriptWorkspace scriptWorkspace) : IEditorPanel
{
    private static readonly ILogger Logger = Log.ForContext<ScriptEditorPanel>();

    private readonly TextEditor _editor = CreateEditor();
    private readonly ScriptEditorSession _session = new();
    private string[] _hints = [];
    private int _hintIndex;
    private string _hintPrefix = "";
    private bool _keysHooked;
    private bool _dockAsTab;
    private bool _focusTab;
    private bool _visible;
    private bool _showCloseConfirm;
    private bool _showSwitchConfirm;
    private bool _showSaveThenConfirm;
    private string? _pendingSwitchName;
    private Action? _saveThenProceed;
    private string _saveThenOkLabel = "Save";

    public bool IsDirty
    {
        get
        {
            SyncTextFromEditor();
            return _session.IsDirty;
        }
    }

    public void Open(string scriptName)
    {
        if (!_visible)
            _dockAsTab = true;
        _focusTab = true;

        var path = scriptWorkspace.GetScriptFilePath(scriptName);
        if (path is null)
        {
            Logger.Warning("Script file not found for {ScriptName}", scriptName);
            return;
        }

        SyncTextFromEditor();
        if (_session.IsOpen
            && !string.Equals(_session.ScriptName, scriptName, StringComparison.Ordinal)
            && _session.IsDirty)
        {
            _pendingSwitchName = scriptName;
            _showSwitchConfirm = true;
            _visible = true;
            return;
        }

        Load(scriptName, path);
    }

    public void RequestSaveThen(Action proceed, string okLabel = "Save")
    {
        SyncTextFromEditor();
        if (!_session.IsDirty)
        {
            proceed();
            return;
        }

        _saveThenProceed = proceed;
        _saveThenOkLabel = okLabel;
        _showSaveThenConfirm = true;
        _visible = true;
    }

    public void Close()
    {
        _session.Close();
        _editor.AllText = "";
        _visible = false;
        _pendingSwitchName = null;
        _saveThenProceed = null;
        _showCloseConfirm = false;
        _showSwitchConfirm = false;
        _showSaveThenConfirm = false;
    }

    public void Draw()
    {
        if (!_visible && !_session.IsOpen)
        {
            DrawModals();
            return;
        }

        _visible = true;
        var visible = _visible;
        var title = WindowTitle();

        if (_dockAsTab)
        {
            ImGui.SetNextWindowDockID(EditorDockspace.DockspaceId, ImGuiCond.Always);
            _dockAsTab = false;
        }

        if (_focusTab)
        {
            ImGui.SetNextWindowFocus();
            _focusTab = false;
        }

        if (ImGui.Begin(title, ref visible))
        {
            EnsureCompletionKeys();
            DrawToolbar();
            var avail = ImGui.GetContentRegionAvail();
            var errorReserve = _session.Errors.Length > 0 ? 80f : 0f;
            var hintReserve = HintListHeight();
            var editorHeight = System.Math.Max(120, avail.Y - errorReserve - hintReserve);
            _editor.Render("##ScriptBody", new Vector2(avail.X, editorHeight));
            RefreshHints();
            DrawHints();
            DrawErrors();
        }

        if (!visible && _visible)
        {
            SyncTextFromEditor();
            if (_session.IsDirty)
            {
                visible = true;
                _showCloseConfirm = true;
            }
            else
                Close();
        }

        _visible = visible;
        ImGui.End();
        DrawModals();
    }
    
    private async Task<bool> Save()
    {
        SyncTextFromEditor();
        if (_session.ScriptName is null || _session.FilePath is null)
            return false;

        var (compileOk, errors) = await scriptWorkspace.CreateOrUpdateScriptAsync(
            _session.ScriptName, _session.Text);

        var persisted = compileOk || FileMatches(_session.FilePath, _session.Text);
        _session.ApplySave(persisted, compileOk, errors);
        return compileOk;
    }

    private void DrawToolbar()
    {
        ButtonDrawer.DrawButton("Save", () => _ = Save());
        ImGui.SameLine();
        ButtonDrawer.DrawButton("Discard", Discard);
    }

    private void DrawErrors()
    {
        if (_session.Errors.Length == 0)
            return;

        ImGui.Separator();
        foreach (var error in _session.Errors)
            TextDrawer.DrawErrorText(error);
    }

    private void EnsureCompletionKeys()
    {
        if (_keysHooked || _editor.Renderer.KeyboardInput is not StandardKeyboardInput keyboard)
            return;

        _keysHooked = true;
        keyboard.AddBinding("Tab", (_, _) =>
        {
            if (_hints.Length > 0)
            {
                AcceptHint(_hints[_hintIndex]);
                return true;
            }

            Indent(false);
            return true;
        });
        keyboard.AddBinding("Shift + Tab", (_, _) =>
        {
            if (_hints.Length > 0)
            {
                _hintIndex = (_hintIndex - 1 + _hints.Length) % _hints.Length;
                return true;
            }

            Indent(true);
            return true;
        });
        keyboard.AddBinding("Enter", (_, _) =>
        {
            if (_hints.Length > 0)
            {
                AcceptHint(_hints[_hintIndex]);
                return true;
            }

            TextEditorModify.EnterCharacter(_editor, '\n');
            return true;
        });
        keyboard.AddBinding("UpArrow", (_, _) =>
        {
            if (_hints.Length == 0)
            {
                _editor.Movement.MoveUp();
                return true;
            }

            _hintIndex = (_hintIndex - 1 + _hints.Length) % _hints.Length;
            return true;
        });
        keyboard.AddBinding("DownArrow", (_, _) =>
        {
            if (_hints.Length == 0)
            {
                _editor.Movement.MoveDown();
                return true;
            }

            _hintIndex = (_hintIndex + 1) % _hints.Length;
            return true;
        });
        keyboard.AddBinding("Escape", (_, _) =>
        {
            if (_hints.Length == 0)
                return false;

            _hints = [];
            return true;
        });

        void Indent(bool shifted)
        {
            if (_editor.Selection.HasSelection && _editor.Selection.Start.Line != _editor.Selection.End.Line)
                TextEditorModify.IndentSelection(_editor, shifted);
            else
                TextEditorModify.EnterCharacter(_editor, '\t');
        }
    }

    private void RefreshHints()
    {
        if (_editor.Selection.HasSelection)
        {
            _hints = [];
            return;
        }

        var prefix = ScriptNameHints.IdentifierPrefix(
            _editor.GetCurrentLineText(),
            _editor.CursorPosition.Column,
            _editor.Options.TabSize);

        if (prefix != _hintPrefix)
        {
            _hintPrefix = prefix;
            _hintIndex = 0;
        }

        var matches = ScriptNameHints.Match(prefix, scriptWorkspace.GetAvailableScriptNames());
        if (matches.Length == 1 && matches[0].Equals(prefix, StringComparison.OrdinalIgnoreCase))
            matches = [];

        _hints = matches;
        if (_hintIndex >= _hints.Length)
            _hintIndex = 0;
    }

    private void DrawHints()
    {
        if (_hints.Length == 0)
            return;

        var height = HintListHeight();
        if (!ImGui.BeginChild("##ScriptHints", new Vector2(0, height), ImGuiChildFlags.Border))
        {
            ImGui.EndChild();
            return;
        }

        for (var i = 0; i < _hints.Length; i++)
        {
            var name = _hints[i];
            if (TreeDrawer.DrawSelectableItem(name, i == _hintIndex, () => AcceptHint(name)))
                _hintIndex = i;
        }

        ImGui.EndChild();
    }

    private float HintListHeight()
    {
        if (_hints.Length == 0)
            return 0;

        return System.Math.Min(_hints.Length, 8) * ImGui.GetTextLineHeightWithSpacing() + 8;
    }

    // ponytail: prefix list, not a language service. Roslyn completion if people need members/locals.
    private void AcceptHint(string name)
    {
        var cursor = _editor.CursorPosition;
        var startCol = cursor.Column - _hintPrefix.Length;
        if (startCol < 0)
            return;

        _editor.Selection.Select((cursor.Line, startCol), cursor);
        TextEditorModify.ReplaceSelection(_editor, name);
        _editor.CursorPosition = (cursor.Line, startCol + name.Length);
        _hints = [];
        _hintPrefix = "";
    }

    private void DrawModals()
    {
        DrawThreeButtonConfirm(
            "Close script",
            ref _showCloseConfirm,
            "This script has unsaved changes.",
            onSave: () => _ = CloseAfterSave(),
            onDiscard: () =>
            {
                Discard();
                Close();
            });

        DrawThreeButtonConfirm(
            "Switch script",
            ref _showSwitchConfirm,
            "Save changes before opening another script?",
            onSave: () => _ = SwitchAfterSave(),
            onDiscard: () =>
            {
                Discard();
                OpenPendingSwitch();
            },
            onCancel: () => _pendingSwitchName = null);

        if (!_showSaveThenConfirm)
            return;

        ModalDrawer.RenderConfirmationModal(
            "Save script",
            ref _showSaveThenConfirm,
            "This script has unsaved changes.",
            onOk: () => _ = SaveThenProceed(),
            onCancel: () => _saveThenProceed = null,
            okLabel: _saveThenOkLabel,
            cancelLabel: "Cancel");
    }

    private async Task CloseAfterSave()
    {
        if (await Save() || !_session.IsDirty)
            Close();
    }

    private async Task SwitchAfterSave()
    {
        if (await Save() || !_session.IsDirty)
            OpenPendingSwitch();
    }

    private async Task SaveThenProceed()
    {
        if (!await Save())
            return;

        var proceed = _saveThenProceed;
        _saveThenProceed = null;
        proceed?.Invoke();
    }

    private void OpenPendingSwitch()
    {
        var name = _pendingSwitchName;
        _pendingSwitchName = null;
        if (name is not null)
            Open(name);
    }

    private void Discard()
    {
        _session.Discard();
        _editor.AllText = _session.Text;
    }

    private void Load(string scriptName, string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to read script {Path}", path);
            return;
        }

        _session.TryLoad(scriptName, path, text);
        _editor.AllText = text;
        _visible = true;
    }

    private void SyncTextFromEditor()
    {
        if (_session.IsOpen)
            _session.Text = _editor.AllText;
    }

    private string WindowTitle()
    {
        var name = _session.ScriptName ?? "Script Editor";
        SyncTextFromEditor();
        return _session.IsDirty ? $"{name} *###ScriptEditor" : $"{name}###ScriptEditor";
    }

    private static bool FileMatches(string path, string text)
    {
        try
        {
            return File.Exists(path) && File.ReadAllText(path) == text;
        }
        catch
        {
            return false;
        }
    }

    private static void DrawThreeButtonConfirm(
        string title,
        ref bool show,
        string message,
        Action onSave,
        Action onDiscard,
        Action? onCancel = null)
    {
        if (!ModalDrawer.BeginCenteredModal(title, ref show))
            return;

        ImGui.TextWrapped(message);
        ImGui.Separator();

        if (ButtonDrawer.DrawModalButton("Save"))
        {
            show = false;
            onSave();
        }

        ImGui.SameLine();
        if (ButtonDrawer.DrawModalButton("Discard"))
        {
            show = false;
            onDiscard();
        }

        ImGui.SameLine();
        if (ButtonDrawer.DrawModalButton("Cancel") || ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            show = false;
            onCancel?.Invoke();
        }

        ModalDrawer.EndModal();
    }

    // ponytail: CStyleHighlighter covers comments and shared C# keywords (class, void, using).
    private static TextEditor CreateEditor()
    {
        var editor = new TextEditor
        {
            SyntaxHighlighter = new CStyleHighlighter(true)
        };
        return editor;
    }
}
