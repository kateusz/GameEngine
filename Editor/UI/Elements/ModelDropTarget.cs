using Editor.Features.Models;
using Editor.Platform;
using Editor.UI.Drawers;
using Engine.Project;
using Engine.Renderer.Models;
using Serilog;

namespace Editor.UI.Elements;

/// <summary>
/// Drag-and-drop target for 3D model files (.glb, .gltf, .fbx) from the content browser.
/// On Windows, clicking opens a native file dialog.
/// </summary>
public static class ModelDropTarget
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ModelDropTarget));
    private static readonly string[] SupportedExtensions = [".glb", ".gltf", ".fbx"];
    private const string FileFilter = "3D Models (*.glb;*.gltf;*.fbx)|*.glb;*.gltf;*.fbx|All files (*.*)|*.*";

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
                : FilePicker.IsAvailable ? "Open..." : "Drop model here";

            if (ButtonDrawer.DrawFullWidthButton(buttonLabel) && FilePicker.IsAvailable)
            {
                string? initial = null;
                try
                {
                    initial = !string.IsNullOrEmpty(currentModelPath)
                        ? PathBuilder.Resolve(currentModelPath)
                        : PathBuilder.AssetsPath;
                }
                catch (InvalidOperationException)
                {
                    initial = Environment.CurrentDirectory;
                }

                var picked = FilePicker.PickFile("Open Model", FileFilter, initial);
                if (!string.IsNullOrEmpty(picked) && IsSupported(picked))
                    LoadModel(picked, modelLoadService, onModelDropped);
            }

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
                    LoadModel(modelPath, modelLoadService, onModelDropped, PathBuilder.ToAssetRelativePath(path));
                });
        });
    }

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
