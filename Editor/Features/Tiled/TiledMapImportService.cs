using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Selection;
using Editor.Panels;
using Engine.Core;
using Engine.Scene;
using Editor.Features.Tiled;
using SceneComponents.Rendering;
using Serilog;

namespace Editor.Features.Tiled;

public sealed class TiledMapImportService(
    ISceneContext sceneContext,
    IEditorHistory history,
    IEditorSelection selection,
    IConsolePanel console)
{
    private static readonly ILogger Logger = Log.ForContext<TiledMapImportService>();

    public void ImportFromContentPath(string contentPath)
    {
        var scene = sceneContext.ActiveScene;
        if (scene is null)
            return;

        var full = PathBuilder.Resolve(contentPath);
        var relative = PathBuilder.ToAssetRelativePath(full);
        if (!TryParse(full, out var data))
            return;

        var command = new ImportTiledMapCommand(scene, data, relative, Path.GetFileNameWithoutExtension(full));
        history.Execute(command);
        if (command.CreatedId is { } id && scene.Context.Contains(id))
            selection.Select(scene.Context.GetById(id), SelectionSource.Code);
    }

    public void Reimport(Entity mapEntity)
    {
        var scene = sceneContext.ActiveScene;
        if (scene is null || !mapEntity.TryGetComponent<TileMapComponent>(out var tilemap))
            return;
        if (string.IsNullOrWhiteSpace(tilemap.SourceMapPath))
        {
            Report("Tilemap has no source .tmj path", error: true);
            return;
        }

        var full = PathBuilder.Resolve(tilemap.SourceMapPath);
        if (!TryParse(full, out var data))
            return;

        history.Execute(new ReimportTiledMapCommand(scene, mapEntity, data, tilemap.SourceMapPath));
    }

    private bool TryParse(string fullPath, out TiledMapData data)
    {
        data = null!;
        var (result, error) = TiledMapParser.FromFile(fullPath, PathBuilder.ToAssetRelativePath);
        if (result is null)
        {
            Report(error ?? "Tiled import failed", error: true);
            return false;
        }

        foreach (var warning in result.Warnings)
            Report(warning, error: false);
        data = result;
        return true;
    }

    private void Report(string message, bool error)
    {
        if (error)
        {
            Logger.Error("{Message}", message);
            console.AddMessage(message, ConsolePanel.LogLevel.Error);
        }
        else
        {
            Logger.Warning("{Message}", message);
            console.AddMessage(message, ConsolePanel.LogLevel.Warning);
        }
    }
}
