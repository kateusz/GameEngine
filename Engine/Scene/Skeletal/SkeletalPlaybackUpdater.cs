using ECS;
using Engine.Core;
using Engine.Renderer;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene.Skeletal;

public static class SkeletalPlaybackUpdater
{
    private static readonly ILogger Logger = Log.ForContext(typeof(SkeletalPlaybackUpdater));
    private static readonly HashSet<int> LoggedMissing = [];

    public static void Tick(IContext context, IModelFactory modelFactory, TimeSpan deltaTime)
    {
        var dt = (float)deltaTime.TotalSeconds;
        foreach (var (entity, playback) in context.View<SkeletalPlaybackComponent>())
        {
            EnsurePalette(playback);

            if (!playback.Playing)
            {
                SkeletalPoseMath.FillIdentity(playback.BonePalette);
                continue;
            }

            if (string.IsNullOrWhiteSpace(playback.MeshPath))
            {
                SkeletalPoseMath.FillIdentity(playback.BonePalette);
                continue;
            }

            var resolved = PathBuilder.Resolve(playback.MeshPath);
            var model = modelFactory.Create(resolved);
            if (model is null || !model.HasSkeleton)
            {
                SkeletalPoseMath.FillIdentity(playback.BonePalette);
                LogOnce(entity.Id, playback.MeshPath, "no skeleton");
                continue;
            }

            var clip = ResolveClip(model, playback.ClipName);
            if (clip is null)
            {
                SkeletalPoseMath.FillIdentity(playback.BonePalette);
                LogOnce(entity.Id, playback.MeshPath, $"unknown clip '{playback.ClipName}'");
                continue;
            }

            playback.Time = SkeletalPoseMath.AdvanceTime(
                playback.Time, dt, playback.Speed, clip.Duration, playback.Loop);
            SkeletalPoseMath.Evaluate(model.Bones, clip, playback.Time, playback.BonePalette);
        }
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

    private static void EnsurePalette(SkeletalPlaybackComponent playback)
    {
        if (playback.BonePalette.Length == SkeletalLimits.MaxBones)
            return;
        playback.BonePalette = SkeletalPlaybackComponent.CreateIdentityPalette();
    }

    private static void LogOnce(int entityId, string? path, string reason)
    {
        if (!LoggedMissing.Add(entityId))
            return;
        Logger.Warning("Skeletal playback bind pose entity={EntityId} path={Path} reason={Reason}",
            entityId, path, reason);
    }
}
