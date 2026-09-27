using Editor.Features.Models;
using Editor.UI.Drawers;
using Engine.Project;
using Engine.Renderer.Models;
using Serilog;

namespace Editor.UI.Elements;

/// <summary>
/// Drag-and-drop target for 3D model files (.glb, .gltf, .fbx) from the content browser.
/// </summary>
public static class ModelDropTarget
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ModelDropTarget));
    private static readonly string[] SupportedExtensions = [".glb", ".gltf", ".fbx"];

    public static bool IsSupported(string path) =>
        DragDropDrawer.HasValidExtension(path, SupportedExtensions);

    public static void Draw(
        string label,
        Action<string, Model> onModelDropped,
        EditorModelLoadService modelLoadService,
        string? currentModelPath = null)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var buttonLabel = !string.IsNullOrEmpty(currentModelPath)
                ? Path.GetFileName(currentModelPath)
                : "Drop model here";

            ButtonDrawer.DrawFullWidthButton(buttonLabel);

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path =>
                {
                    var modelPath = PathBuilder.Resolve(path);
                    return DragDropDrawer.IsValidFile(modelPath, SupportedExtensions);
                },
                path =>
                {
                    var modelPath = PathBuilder.Resolve(path);
                    var relative = PathBuilder.ToAssetRelativePath(path);
                    modelLoadService.Request(modelPath, model =>
                    {
                        if (model == null)
                        {
                            Logger.Warning("Failed to load model from {Path}", modelPath);
                            return;
                        }

                        onModelDropped(relative, model);
                    });
                });
        });
    }
}
