using Serilog.Core;
using Serilog.Events;

namespace Editor.Logging;

/// <summary>
/// Enriches log events with the current active scene name.
/// </summary>
public class SceneNameEnricher : ILogEventEnricher
{
    private static string? _currentSceneName;

    /// <summary>
    /// Sets the current active scene name.
    /// </summary>
    public static void SetSceneName(string? sceneName)
    {
        _currentSceneName = sceneName;
    }

    /// <summary>
    /// Gets the current scene name.
    /// </summary>
    public static string? CurrentSceneName => _currentSceneName;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var sceneName = _currentSceneName ?? "None";
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Scene", sceneName));
    }
}
