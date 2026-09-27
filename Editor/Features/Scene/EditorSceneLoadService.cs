using System.Text.Json.Nodes;
using Serilog;

namespace Editor.Features.Scene;

public sealed class EditorSceneLoadService(SceneManager sceneManager)
{
    private static readonly ILogger Logger = Log.ForContext<EditorSceneLoadService>();

    private bool _holdForPaint;
    private Task<(string Path, JsonObject? Root)>? _readTask;

    public bool IsLoading { get; private set; }

    public string? LoadingName { get; private set; }

    public void Request(string resolvedPath)
    {
        if (IsLoading)
            return;

        var normalized = Path.GetFullPath(resolvedPath);
        IsLoading = true;
        _holdForPaint = true;
        LoadingName = Path.GetFileNameWithoutExtension(normalized);
        _readTask = Task.Run(() =>
        {
            try
            {
                var json = File.ReadAllText(normalized);
                var root = string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json)?.AsObject();
                return (normalized, root);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to read scene: {Path}", normalized);
                return (normalized, null);
            }
        });
    }

    public void Pump()
    {
        // skip one Pump so the overlay frame is presented before sync Open blocks the swap.
        if (_holdForPaint)
        {
            _holdForPaint = false;
            return;
        }

        if (_readTask is not { IsCompleted: true } task)
            return;

        _readTask = null;
        try
        {
            var (path, root) = task.GetAwaiter().GetResult();
            if (root is not null)
                sceneManager.Open(path, root);
            else
                Logger.Error("Invalid or empty scene file: {Path}", path);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open scene");
        }
        finally
        {
            IsLoading = false;
            LoadingName = null;
        }
    }
}
