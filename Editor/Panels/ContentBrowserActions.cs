using Editor.Features.Components;
using Editor.Features.Scripting;
using Engine.Core;
using Engine.Scripting;
using Serilog;

namespace Editor.Panels;

public class ContentBrowserActions(
    IProjectContext projectContext,
    GameScriptWorkspace scriptWorkspace,
    IGameComponentFactory gameComponentFactory)
{
    private static readonly ILogger Logger = Log.ForContext<ContentBrowserActions>();

    public async Task<(bool Success, string? Error)> CreateScriptAsync(string scriptName)
    {
        if (projectContext.ScriptsDir is null)
            return (false, "Open a project first.");

        var template = ScriptableEntityTemplates.Generate(scriptName);
        var (success, errors) = await scriptWorkspace.CreateOrUpdateScriptAsync(scriptName, template);
        return success ? (true, null) : (false, string.Join('\n', errors.Take(5)));
    }

    public async Task<(bool Success, string? Error)> CreateSystemAsync(string baseName)
    {
        if (projectContext.ScriptsDir is not { } scriptsDir)
            return (false, "Open a project first.");

        var className = GameSystemTemplates.ToClassName(baseName);
        var filePath = Path.Combine(scriptsDir, $"{className}.cs");
        if (File.Exists(filePath))
            return (false, "System file already exists.");

        await File.WriteAllTextAsync(filePath, GameSystemTemplates.Generate(className));
        Logger.Information("Created game system file {Path}", filePath);

        var (compiled, errors) = scriptWorkspace.TryCompileAllScripts();
        return compiled ? (true, null) : (false, string.Join('\n', errors.Take(5)));
    }

    public Task<(bool Success, string? Error)> CreateComponentAsync(string baseName) =>
        gameComponentFactory.CreateFileAsync(baseName);
}
