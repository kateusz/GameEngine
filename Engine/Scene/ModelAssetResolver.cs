using ECS;
using Engine.Core;
using Engine.Renderer;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene;

internal static class ModelAssetResolver
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ModelAssetResolver));
    private const string BuiltinSphereModelPath = "builtin:sphere";
    private static readonly HashSet<string> WarnedPaths = [];

    public static void SyncAll(IContext context, IModelFactory modelFactory)
    {
        foreach (var entity in context.Entities)
            SyncEntity(entity, ResolveMeshPath(entity), modelFactory);
    }

    private static string? ResolveMeshPath(Entity entity)
    {
        if (entity.TryGetComponent<ModelRendererComponent>(out var renderer)
            && !string.IsNullOrWhiteSpace(renderer.ModelPath)
            && !IsBuiltin(renderer.ModelPath))
            return renderer.ModelPath;

        if (entity.TryGetComponent<SkeletalPlaybackComponent>(out var playback)
            && !string.IsNullOrWhiteSpace(playback.MeshPath))
            return playback.MeshPath;

        return null;
    }

    private static void SyncEntity(Entity entity, string? path, IModelFactory modelFactory)
    {
        if (path is null)
        {
            if (entity.HasComponent<ResolvedModelComponent>())
                entity.RemoveComponent<ResolvedModelComponent>();
            return;
        }

        if (!entity.TryGetComponent<ResolvedModelComponent>(out var resolved))
            resolved = entity.AddComponent<ResolvedModelComponent>();

        if (string.Equals(resolved.SourcePath, path, StringComparison.Ordinal))
            return;

        resolved.SourcePath = path;
        resolved.Model = LoadModel(path, modelFactory);
    }

    private static Model? LoadModel(string assetPath, IModelFactory modelFactory)
    {
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

    private static bool IsBuiltin(string path) =>
        string.Equals(path, BuiltinSphereModelPath, StringComparison.OrdinalIgnoreCase);

    private static void WarnOnce(string path, Action log)
    {
        if (!WarnedPaths.Add(path))
            return;
        log();
    }
}
