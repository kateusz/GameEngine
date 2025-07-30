namespace Editor.Windows.ScriptEditor;

public class ScriptEditorState
{
    public bool IsOpen { get; set; } = false;
    public string ScriptName { get; private set; } = string.Empty;
    public string ScriptContent { get; private set; } = string.Empty;
    public string OriginalScriptContent { get; private set; } = string.Empty;
    public bool IsNewScript { get; private set; } = false;
    public bool HasChanges { get; private set; } = false;
    public string ErrorMessage { get; set; } = string.Empty;
    public Action<bool> OnCloseCallback { get; private set; }

    public string WindowTitle
    {
        get
        {
            string title = IsNewScript ? $"New Script: {ScriptName}" : $"Edit Script: {ScriptName}";
            return HasChanges ? title + "*" : title;
        }
    }

    public void Open(string scriptName, bool isNewScript, Action<bool> onCloseCallback)
    {
        ScriptName = scriptName;
        IsNewScript = isNewScript;
        IsOpen = true;
        ErrorMessage = string.Empty;
        OnCloseCallback = onCloseCallback;
        HasChanges = false;
    }

    public void Close()
    {
        IsOpen = false;
        ScriptName = string.Empty;
        ScriptContent = string.Empty;
        OriginalScriptContent = string.Empty;
        ErrorMessage = string.Empty;
        HasChanges = false;
        IsNewScript = false;
        OnCloseCallback = null;
    }

    public void SetContent(string content, bool isOriginal = false)
    {
        ScriptContent = content;
        if (isOriginal)
        {
            OriginalScriptContent = content;
            HasChanges = false;
        }
        else
        {
            HasChanges = ScriptContent != OriginalScriptContent;
        }
    }

    public void UpdateContent(string newContent)
    {
        ScriptContent = newContent;
        HasChanges = ScriptContent != OriginalScriptContent;
    }

    public void MarkAsSaved()
    {
        OriginalScriptContent = ScriptContent;
        HasChanges = false;
        ErrorMessage = string.Empty;
    }

    public void SetError(string error)
    {
        ErrorMessage = error;
    }

    public void ClearError()
    {
        ErrorMessage = string.Empty;
    }

    public void MarkAsOpened()
    {
        // Called when the text editor has been focused for the first time
        IsNewScript = false;
    }
}