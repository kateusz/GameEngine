using Editor.AssetPicker;
using Editor.Features.Models;
using Editor.UI.Drawers;
using Engine.Project;
using Engine.Renderer.Models;
using Serilog;

namespace Editor.UI.Elements;

/// <summary>
/// Model asset field: opens Asset Browser; Content Browser drop still supported.
/// </summary>
public class ModelDropTarget(AssetPathField assetPathField)
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ModelDropTarget));
    private static readonly string[] SupportedExtensions = AssetKind.Model.Extensions;

    public static bool IsSupported(string path) =>
        DragDropDrawer.HasValidExtension(path, SupportedExtensions);

    public void Draw(
        string label,
        Action<string, Model> onModelDropped,
        EditorModelLoadService modelLoadService,
        string? currentModelPath = null) =>
        assetPathField.Draw(
            label,
            AssetKind.Model,
            currentModelPath,
            relative => LoadModel(PathBuilder.Resolve(relative), modelLoadService, onModelDropped, relative));

    private static void LoadModel(
        string absolutePath,
        EditorModelLoadService modelLoadService,
        Action<string, Model> onModelDropped,
        string? relativePath = null)
    {
        var relative = relativePath ?? PathBuilder.ToAssetRelativePath(absolutePath);
        modelLoadService.Request(absolutePath, model =>
        {
            if (model == null)
            {
                Logger.Warning("Failed to load model from {Path}", absolutePath);
                return;
            }

            onModelDropped(relative, model);
        });
    }
}
