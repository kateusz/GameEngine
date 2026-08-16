using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Scene;
using SceneComponents;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene.Skeletal;

public static class SkeletalPlaybackUpdater
{
    private static readonly ILogger Logger = Log.ForContext(typeof(SkeletalPlaybackUpdater));
    private static readonly HashSet<int> LoggedMissing = [];

    public static void Tick(IContext context, TimeSpan deltaTime, IModelFactory modelFactory)
    {
        var dt = (float)deltaTime.TotalSeconds;
        foreach (var (entity, playback) in context.View<SkeletalPlaybackComponent>())
            UpdatePlayback(entity, playback, dt, modelFactory);

        foreach (var (entity, renderer) in context.View<ModelRendererComponent>())
            BindSkinning(context, entity, renderer, modelFactory);
    }

    public static AnimationClip? ResolveClip(Model model, string? clipName)
    {
        if (model.Clips.Count == 0)
            return null;
        if (string.IsNullOrWhiteSpace(clipName))
            return model.Clips[0];
        foreach (var clip in model.Clips)
        {
            if (string.Equals(clip.Name, clipName, StringComparison.Ordinal))
                return clip;
        }

        return null;
    }

    private static void UpdatePlayback(
        Entity entity, SkeletalPlaybackComponent playback, float dt, IModelFactory modelFactory)
    {
        EnsurePalette(playback);

        if (!playback.Playing || string.IsNullOrWhiteSpace(playback.MeshPath))
        {
            SkeletalPoseMath.FillIdentity(playback.BonePalette);
            return;
        }

        var model = MeshAsset.TryLoad(modelFactory, playback.MeshPath);
        if (model is null || !model.HasSkeleton)
        {
            SkeletalPoseMath.FillIdentity(playback.BonePalette);
            LogOnce(entity.Id, playback.MeshPath, "no skeleton");
            return;
        }

        var clip = ResolveClip(model, playback.ClipName);
        if (clip is null)
        {
            SkeletalPoseMath.FillIdentity(playback.BonePalette);
            LogOnce(entity.Id, playback.MeshPath, $"unknown clip '{playback.ClipName}'");
            return;
        }

        playback.Time = SkeletalPoseMath.AdvanceTime(
            playback.Time, dt, playback.Speed, clip.Duration, playback.Loop);
        SkeletalPoseMath.Evaluate(model.Bones, clip, playback.Time, playback.BonePalette);
    }

    private static void BindSkinning(
        IContext context, Entity entity, ModelRendererComponent renderer, IModelFactory modelFactory)
    {
        renderer.BonePalette = null;
        renderer.SkinningWorld = Matrix4x4.Identity;

        if (string.IsNullOrWhiteSpace(renderer.ModelPath))
            return;

        var current = entity;
        while (true)
        {
            if (current.TryGetComponent<SkeletalPlaybackComponent>(out var playback)
                && PathsEqual(playback.MeshPath, renderer.ModelPath)
                && current.TryGetComponent<TransformComponent>(out var transform))
            {
                var model = MeshAsset.TryLoad(modelFactory, renderer.ModelPath);
                if (model is { HasSkeleton: true })
                {
                    renderer.BonePalette = playback.BonePalette;
                    renderer.SkinningWorld = transform.GetWorldTransform();
                }

                return;
            }

            if (!current.TryGetComponent<ParentComponent>(out var parent)
                || parent.ParentId is not int parentId
                || !context.Contains(parentId))
                return;

            current = context.GetById(parentId);
        }
    }

    private static void EnsurePalette(SkeletalPlaybackComponent playback)
    {
        if (playback.BonePalette.Length == SkeletalLimits.MaxBones)
            return;
        playback.BonePalette = SkeletalPlaybackComponent.CreateIdentityPalette();
    }

    private static bool PathsEqual(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a)
        && !string.IsNullOrWhiteSpace(b)
        && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static void LogOnce(int entityId, string? path, string reason)
    {
        if (!LoggedMissing.Add(entityId))
            return;
        Logger.Warning("Skeletal playback bind pose entity={EntityId} path={Path} reason={Reason}",
            entityId, path, reason);
    }
}
