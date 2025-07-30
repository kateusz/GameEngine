using Engine.Scripting;
using NLog;

namespace Editor.Windows.ScriptEditor;

public class ScriptFileManager : IScriptFileProvider
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public void LoadScript(ScriptEditorState state)
    {
        try
        {
            var content = LoadScriptAsync(state.ScriptName, state.IsNewScript).Result;
            state.SetContent(content, isOriginal: true);
            state.ClearError();
        }
        catch (Exception ex)
        {
            state.SetError($"Error loading script: {ex.Message}");
            Logger.Error(ex, "Error loading script");
        }
    }

    public async Task<bool> SaveScript(ScriptEditorState state)
    {
        try
        {
            Console.WriteLine($"Saving script: {state.ScriptName}"); // Debug
            var (success, errors) = await SaveScriptAsync(state.ScriptName, state.ScriptContent);

            if (success)
            {
                Console.WriteLine("Script save successful"); // Debug
                state.MarkAsSaved();
                Logger.Info($"Script '{state.ScriptName}' saved successfully");
                return true;
            }
            else
            {
                Console.WriteLine($"Script save failed: {string.Join(", ", errors)}"); // Debug
                state.SetError($"Compilation failed: {string.Join("\n", errors)}");
                Logger.Error($"Script compilation failed: {string.Join(", ", errors)}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Script save exception: {ex.Message}"); // Debug
            state.SetError($"Error saving script: {ex.Message}");
            Logger.Error(ex, "Error saving script");
            return false;
        }
    }

    // Interface implementations for better testability
    public async Task<string> LoadScriptAsync(string scriptName, bool isNewScript)
    {
        if (isNewScript)
        {
            return GenerateTemplate(scriptName);
        }
        else
        {
            // Wrap synchronous call in Task for interface consistency
            return await Task.FromResult(ScriptEngine.Instance.GetScriptSource(scriptName));
        }
    }

    public async Task<(bool success, string[] errors)> SaveScriptAsync(string scriptName, string content)
    {
        return await ScriptEngine.Instance.CreateOrUpdateScriptAsync(scriptName, content);
    }

    public string GenerateTemplate(string scriptName)
    {
        return ScriptEngine.GenerateScriptTemplate(scriptName);
    }
}