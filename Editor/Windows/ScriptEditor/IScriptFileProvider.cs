namespace Editor.Windows.ScriptEditor;

public interface IScriptFileProvider
{
    Task<string> LoadScriptAsync(string scriptName, bool isNewScript);
    Task<(bool success, string[] errors)> SaveScriptAsync(string scriptName, string content);
    string GenerateTemplate(string scriptName);
}