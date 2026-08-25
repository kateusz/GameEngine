namespace Editor.Features.Scripting;

internal sealed class ScriptEditorSession
{
    public string? ScriptName { get; private set; }
    public string? FilePath { get; private set; }
    public string Text { get; set; } = "";
    public string Snapshot { get; private set; } = "";
    public string[] Errors { get; private set; } = [];
    public bool IsOpen { get; private set; }

    public bool IsDirty => IsOpen && Text != Snapshot;

    public bool TryLoad(string name, string? path, string? text)
    {
        if (string.IsNullOrWhiteSpace(name) || path is null || text is null)
            return false;

        ScriptName = name;
        FilePath = path;
        Text = text;
        Snapshot = text;
        Errors = [];
        IsOpen = true;
        return true;
    }

    public void Discard()
    {
        Text = Snapshot;
        Errors = [];
    }

    public void ApplySave(bool persisted, bool compileOk, string[] errors)
    {
        Errors = compileOk ? [] : errors;
        if (persisted)
            Snapshot = Text;
    }

    public void Close()
    {
        IsOpen = false;
        ScriptName = null;
        FilePath = null;
        Text = "";
        Snapshot = "";
        Errors = [];
    }
}
