using System.Reflection;
using Serilog;

namespace Engine.Scripting;

internal sealed class ScriptEngine : IScriptEngine
{
    private static readonly ILogger Logger = Log.ForContext<ScriptEngine>();

    private Assembly? _dynamicAssembly;
    private GameAssemblyLoadContext? _loadContext;

    public void LoadGameAssemblyFromFile(string dllPath)
    {
        var loadedDllPath = Path.GetFullPath(dllPath);
        if (!File.Exists(loadedDllPath))
        {
            Logger.Error("Game assembly not found: {Path}", loadedDllPath);
            return;
        }

        try
        {
            UnloadLoadContext();
            _loadContext = new GameAssemblyLoadContext(loadedDllPath);
            _dynamicAssembly = _loadContext.LoadAssembly();
            Logger.Information("Loaded game assembly from {Path}", loadedDllPath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load game assembly from {Path}", loadedDllPath);
        }
    }

    public Assembly? GetLoadedGameAssembly() => _dynamicAssembly;

    public void UnloadGameAssembly() => UnloadLoadContext();

    private void UnloadLoadContext()
    {
        _dynamicAssembly = null;

        if (_loadContext is null)
            return;

        _loadContext.Unload();
        _loadContext = null;
    }
}
