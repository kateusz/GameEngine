using System.Text.Json.Nodes;
using Editor.Features.Models;
using Editor.UI.Drawers;
using Engine.Project;
using Engine.Scene;
using SceneComponents.Rendering;
using Serilog;

namespace Editor.Features.Scene;

public sealed class EditorSceneLoadService(
    SceneManager sceneManager,
    EditorModelLoadService modelLoadService,
    ISceneContext sceneContext)
{
    private static readonly ILogger Logger = Log.ForContext<EditorSceneLoadService>();

    private bool _holdForPaint;
    private Task<(string Path, JsonObject Root)>? _readTask;
    private int _modelsRemaining;
    private bool _warmingModels;
    private readonly EditorLoadWorkMeter _work = new();

    private bool _showMessage;
    private string _messageTitle = "";
    private string _messageBody = "";
    private MessageType _messageType = MessageType.Info;

    public bool IsLoading { get; private set; }

    public string? LoadingName { get; private set; }

    /// <summary>Null while reading JSON or before first work unit is registered.</summary>
    public float? LoadProgress =>
        _warmingModels ? _work.Progress : null;

    public string LoadingStatusText
    {
        get
        {
            var scene = string.IsNullOrEmpty(LoadingName) ? "scene" : LoadingName;
            if (!_warmingModels || _work.Total == 0)
                return $"Loading scene {scene}...";

            var text = $"Loading scene {scene}... ({_work.Done}/{_work.Total})";
            var busy = modelLoadService.BusyName;
            return string.IsNullOrEmpty(busy) ? text : $"{text} — {busy}";
        }
    }

    public void Request(string resolvedPath)
    {
        if (IsLoading)
            return;

        var normalized = Path.GetFullPath(resolvedPath);
        IsLoading = true;
        _holdForPaint = true;
        _warmingModels = false;
        _modelsRemaining = 0;
        _work.Reset();
        LoadingName = Path.GetFileNameWithoutExtension(normalized);
        _readTask = Task.Run(() =>
        {
            var json = File.ReadAllText(normalized);
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException($"Scene file is empty: {normalized}");

            var root = JsonNode.Parse(json)?.AsObject()
                       ?? throw new InvalidOperationException($"Scene file is not valid JSON: {normalized}");
            return (normalized, root);
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

        if (_warmingModels)
        {
            if (Volatile.Read(ref _modelsRemaining) > 0)
                return;

            _warmingModels = false;
            _work.Reset();
            IsLoading = false;
            LoadingName = null;
            return;
        }

        if (_readTask is not { IsCompleted: true } task)
            return;

        _readTask = null;
        try
        {
            var (path, root) = task.GetAwaiter().GetResult();
            var skipped = sceneManager.Open(path, root);
            if (skipped.Count > 0)
            {
                var list = string.Join("\n", skipped.Select(n => $"• {n}"));
                ShowMessage(
                    "Unknown Components Skipped",
                    $"The scene loaded, but these unknown components were skipped:\n\n{list}",
                    MessageType.Warning);
            }

            BeginModelWarmup();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open scene");
            ShowMessage(
                "Scene Load Failed",
                $"Could not load the scene.\n\n{ex.Message}",
                MessageType.Error);
            IsLoading = false;
            LoadingName = null;
        }
    }

    public void Render()
    {
        if (!_showMessage)
            return;

        ModalDrawer.RenderMessageBox(_messageTitle, ref _showMessage, _messageBody, _messageType);
    }

    private void BeginModelWarmup()
    {
        var paths = CollectModelPaths();
        if (paths.Count == 0)
        {
            IsLoading = false;
            LoadingName = null;
            return;
        }

        _warmingModels = true;
        Volatile.Write(ref _modelsRemaining, paths.Count);
        foreach (var path in paths)
            modelLoadService.Request(path, _ => Interlocked.Decrement(ref _modelsRemaining), _work);
    }

    private List<string> CollectModelPaths()
    {
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scene = sceneContext.ActiveScene;
        if (scene == null)
            return [];

        foreach (var (_, renderer) in scene.Context.View<ModelRendererComponent>())
        {
            if (string.IsNullOrWhiteSpace(renderer.ModelPath))
                continue;

            var resolved = PathBuilder.Resolve(renderer.ModelPath);
            if (File.Exists(resolved))
                unique.Add(Path.GetFullPath(resolved));
        }

        return [.. unique];
    }

    private void ShowMessage(string title, string body, MessageType type)
    {
        _messageTitle = title;
        _messageBody = body;
        _messageType = type;
        _showMessage = true;
    }
}
