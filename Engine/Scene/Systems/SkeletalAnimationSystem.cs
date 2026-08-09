using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Renderer.Skeletal;
using Engine.Scene.Skeletal;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class SkeletalAnimationSystem(
    IContext context,
    ISkeletonFactory skeletonFactory,
    IAnim3dFactory anim3dFactory) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SkeletalAnimationSystem>();

    public int Priority => SystemPriorities.SkeletalAnimationSystem;

    public void OnInit() =>
        Logger.Debug("SkeletalAnimationSystem initialized with priority {Priority}", Priority);

    public void OnUpdate(TimeSpan deltaTime)
    {
        foreach (var (entity, playback) in context.View<SkeletalPlaybackComponent>())
        {
            EnsurePalette(playback);

            if (!playback.Playing
                || string.IsNullOrWhiteSpace(playback.SkeletonPath)
                || string.IsNullOrWhiteSpace(playback.ClipPath))
            {
                if (playback.Playing)
                {
                    SkinnedRenderDiagnostics.Once(
                        $"anim-skip-{entity.Id}",
                        () => Logger.Warning(
                            "SkinnedDbg anim skipped entity={EntityId} Playing=true but Skeleton={Skeleton} Clip={Clip}",
                            entity.Id, playback.SkeletonPath ?? "(null)", playback.ClipPath ?? "(null)"));
                }

                FillIdentity(playback.BonePalette);
                continue;
            }

            SkeletonAsset? skeleton;
            Anim3dAsset? anim;
            try
            {
                skeleton = skeletonFactory.Create(PathBuilder.Resolve(playback.SkeletonPath));
                anim = anim3dFactory.Create(PathBuilder.Resolve(playback.ClipPath));
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to resolve skeletal assets for playback");
                FillIdentity(playback.BonePalette);
                continue;
            }

            if (skeleton is null || anim is null || anim.Clips.Count == 0)
            {
                SkinnedRenderDiagnostics.Once(
                    $"anim-empty-{entity.Id}",
                    () => Logger.Warning(
                        "SkinnedDbg anim assets empty entity={EntityId} skeletonNull={SkelNull} animNull={AnimNull} clipCount={ClipCount}",
                        entity.Id, skeleton is null, anim is null, anim?.Clips.Count ?? 0));
                FillIdentity(playback.BonePalette);
                continue;
            }

            var clip = ResolveClip(anim, playback.ClipName);
            if (clip is null)
            {
                SkinnedRenderDiagnostics.Once(
                    $"anim-clip-{entity.Id}",
                    () => Logger.Warning(
                        "SkinnedDbg clip not found entity={EntityId} ClipName={ClipName} available=[{Names}]",
                        entity.Id,
                        playback.ClipName ?? "(null)",
                        string.Join(", ", anim.Clips.Select(c => c.Name))));
                FillIdentity(playback.BonePalette);
                continue;
            }

            playback.Time += (float)deltaTime.TotalSeconds * playback.Speed;
            var duration = clip.DurationSeconds;
            if (duration > 0f)
            {
                if (playback.Loop)
                {
                    playback.Time %= duration;
                    if (playback.Time < 0f)
                        playback.Time += duration;
                }
                else
                {
                    playback.Time = System.Math.Clamp(playback.Time, 0f, duration);
                }
            }

            SkeletalPoseMath.Evaluate(skeleton, clip, playback.Time, playback.BonePalette);

            if (SkinnedRenderDiagnostics.DebugEnabled)
            {
                SkinnedRenderDiagnostics.Once(
                    $"anim-eval-{entity.Id}",
                    () =>
                    {
                        var bindPalette = new Matrix4x4[SkeletalPoseMath.MaxBones];
                        SkeletalPoseMath.Evaluate(
                            skeleton,
                            new Anim3dClip("__bind_check", clip.DurationSeconds, []),
                            0f,
                            bindPalette);
                        var bindDev = 0f;
                        for (var i = 0; i < skeleton.Bones.Count; i++)
                            bindDev = MathF.Max(bindDev, SkinnedRenderDiagnostics.MatrixDeviationFromIdentity(bindPalette[i]));

                        var t0 = 0f;
                        foreach (var ch in clip.Channels)
                            t0 = MathF.Max(t0, SkeletalPoseMath.ChannelBindTime(ch));
                        var t0Palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
                        SkeletalPoseMath.Evaluate(skeleton, clip, t0, t0Palette);
                        var t0Dev = 0f;
                        for (var i = 0; i < skeleton.Bones.Count; i++)
                            t0Dev = MathF.Max(t0Dev, SkinnedRenderDiagnostics.MatrixDeviationFromIdentity(t0Palette[i]));

                        Logger.Debug(
                            "SkinnedDbg anim evaluate entity={EntityId} bones={BoneCount} clip={Clip} time={Time:F3}s duration={Duration:F3}s channels={Channels} restPaletteMaxDev={BindDev:F4} t0PaletteMaxDev={T0Dev:F4} t0={T0:F4}",
                            entity.Id, skeleton.Bones.Count, clip.Name, playback.Time, clip.DurationSeconds, clip.Channels.Count, bindDev, t0Dev, t0);
                        SkinnedRenderDiagnostics.LogBonePalette($"anim-entity-{entity.Id}", playback.BonePalette, 3, 7, 13, 17);
                    });

                if (SkinnedRenderDiagnostics.EveryNFrames(120))
                {
                    Logger.Debug(
                        "SkinnedDbg anim tick entity={EntityId} time={Time:F3}s clip={Clip}",
                        entity.Id, playback.Time, clip.Name);
                    SkinnedRenderDiagnostics.LogBonePalette($"anim-tick-{entity.Id}", playback.BonePalette, 3, 7, 13, 17);
                }
            }
        }
    }

    public void OnShutdown() { }

    private static Anim3dClip? ResolveClip(Anim3dAsset anim, string? clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return anim.Clips[0];

        foreach (var clip in anim.Clips)
        {
            if (string.Equals(clip.Name, clipName, StringComparison.Ordinal))
                return clip;
        }

        return null;
    }

    private static void EnsurePalette(SkeletalPlaybackComponent playback)
    {
        if (playback.BonePalette.Length != SkeletalPlaybackComponent.MaxBones)
            playback.BonePalette = SkeletalPlaybackComponent.CreateIdentityPalette();
    }

    private static void FillIdentity(Matrix4x4[] palette)
    {
        for (var i = 0; i < palette.Length; i++)
            palette[i] = Matrix4x4.Identity;
    }
}
