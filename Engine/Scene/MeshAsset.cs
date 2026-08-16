using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Models;
using Serilog;

namespace Engine.Scene;

internal static class MeshAsset
{
    private static readonly ILogger Logger = Log.ForContext(typeof(MeshAsset));
    private static readonly HashSet<string> WarnedPaths = [];

    public static Model? TryLoad(IModelFactory modelFactory, string? assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return null;

        string resolvedPath;
        try
        {
            resolvedPath = PathBuilder.Resolve(assetPath);
        }
        catch (Exception ex)
        {
            WarnOnce(assetPath, () =>
                Logger.Warning(ex, "Failed to resolve model path assetPath={AssetPath}", assetPath));
            return null;
        }

        var model = modelFactory.Create(resolvedPath);
        if (model is null)
        {
            WarnOnce(assetPath, () =>
                Logger.Warning(
                    "Failed to load model assetPath={AssetPath} resolved={ResolvedPath} — consumers use fail-soft fallback",
                    assetPath, resolvedPath));
        }

        return model;
    }

    private static void WarnOnce(string path, Action log)
    {
        if (!WarnedPaths.Add(path))
            return;
        log();
    }
}
