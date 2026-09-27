using System.Text.Json.Nodes;
using Editor.UI.Drawers;
using Serilog;

namespace Editor.Features.Scene;

public sealed class EditorSceneLoadService(SceneManager sceneManager)
{
    private static readonly ILogger Logger = Log.ForContext<EditorSceneLoadService>();

    private bool _holdForPaint;
    private Task<(string Path, JsonObject Root)>? _readTask;

    private bool _showMessage;
    private string _messageTitle = "";
    private string _messageBody = "";
    private MessageType _messageType = MessageType.Info;

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
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open scene");
            ShowMessage(
                "Scene Load Failed",
                $"Could not load the scene.\n\n{ex.Message}",
                MessageType.Error);
        }
        finally
        {
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

    private void ShowMessage(string title, string body, MessageType type)
    {
        _messageTitle = title;
        _messageBody = body;
        _messageType = type;
        _showMessage = true;
    }
}
