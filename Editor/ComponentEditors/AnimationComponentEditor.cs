using ECS;
using Editor.ComponentEditors.Core;
using Editor.Panels;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Animation;
using Engine.Scene.Components;
using ImGuiNET;
using Serilog;

namespace Editor.ComponentEditors;

/// <summary>
/// Inspector editor for AnimationComponent.
/// Provides UI for asset selection, clip control, playback settings, and frame scrubbing.
/// </summary>
public class AnimationComponentEditor(
    IAnimationAssetManager animationAssetManager,
    AnimationTimelinePanel? timelineWindow)
    : IComponentEditor
{
    private static readonly ILogger Logger = Log.ForContext<AnimationComponentEditor>();

    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<AnimationComponent>("Animation", e, () =>
        {
            var component = e.GetComponent<AnimationComponent>();

            if (component.Asset is null)
            {
                component.Asset = animationAssetManager.LoadAsset(component.AssetPath);
            }

            DrawAssetPath(e, component);
            ImGui.Spacing();

            if (component.Asset != null)
            {
                DrawClipSelector(e, component);
                ImGui.Spacing();

                DrawPlaybackControls(e, component);
                ImGui.Spacing();

                DrawTimeline(e, component);
                ImGui.Spacing();

                DrawFrameInfo(component);
                ImGui.Spacing();

                DrawClipList(e, component);
                ImGui.Spacing();

                DrawActionButtons(e, component);
            }
            else
            {
                TextDrawer.DrawWarningText("No animation asset loaded");
            }
        });
    }

    private void DrawAssetPath(Entity entity, AnimationComponent component)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - EditorUIConstants.StandardButtonWidth -
                               EditorUIConstants.StandardPadding);

        ImGui.Text($"Asset Path: {component.AssetPath ?? "(none)"}");

        UIPropertyRenderer.DrawPropertyRow("Animation", () =>
        {
            ButtonDrawer.DrawFullWidthButton("Browse...", () =>
            {
                // TODO: Open file browser popup (Phase 3 enhancement)
                Logger.Information("File browser not yet implemented");
            });

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path => !string.IsNullOrWhiteSpace(path) &&
                       DragDropDrawer.HasValidExtension(path, ".anim"),
                droppedPath =>
                {
                    // Unload previous asset if exists
                    if (component.Asset != null && !string.IsNullOrWhiteSpace(component.AssetPath))
                    {
                        animationAssetManager.UnloadAsset(component.AssetPath);
                    }

                    // Load new asset
                    var animation = animationAssetManager.LoadAsset(droppedPath);

                    component.AssetPath = droppedPath;
                    component.Asset = animation;

                    // Auto-play first clip if available
                    if (animation is { Clips.Length: > 0 })
                    {
                        AnimationController.Play(entity, animation.Clips[0].Name);
                    }
                });
        });

        // Show load status
        if (component.Asset == null && !string.IsNullOrEmpty(component.AssetPath))
        {
            TextDrawer.DrawErrorText("Failed to load");
        }
    }

    private void DrawClipSelector(Entity entity, AnimationComponent component)
    {
        var asset = component.Asset!;
        var currentClip = asset.GetClip(component.CurrentClipName);

        var clipNames = asset.Clips.Select(c => c.Name).ToArray();
        LayoutDrawer.DrawComboBox("Current Clip", component.CurrentClipName, clipNames,
            selectedClip => AnimationController.Play(entity, selectedClip));

        // Clip info
        if (currentClip != null)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"({currentClip.Frames.Length} frames, {currentClip.Duration:F2}s)");
        }
    }

    private void DrawPlaybackControls(Entity entity, AnimationComponent component)
    {
        ImGui.Text("Playback:");
        ImGui.SameLine();

        // Playing checkbox
        var isPlaying = component.IsPlaying;
        if (ImGui.Checkbox("Playing", ref isPlaying))
        {
            if (isPlaying)
                AnimationController.Resume(entity);
            else
                AnimationController.Pause(entity);
        }

        ImGui.SameLine();

        // Loop checkbox
        var loop = component.Loop;
        if (ImGui.Checkbox("Loop", ref loop))
        {
            component.Loop = loop;
        }

        ImGui.SameLine();

        // Speed slider
        ImGui.Text("Speed:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(EditorUIConstants.FilterInputWidth);
        var speed = component.PlaybackSpeed;
        if (ImGui.SliderFloat("##Speed", ref speed, 0.1f, 3.0f, "%.1fx"))
        {
            AnimationController.SetSpeed(entity, speed);
        }
    }

    private void DrawTimeline(Entity entity, AnimationComponent component)
    {
        var clip = component.Asset!.GetClip(component.CurrentClipName);
        if (clip == null) return;

        ImGui.Text("Timeline:");

        // Play button
        ButtonDrawer.DrawSmallButton(component.IsPlaying ? "⏸" : "▶", () =>
        {
            if (component.IsPlaying)
                AnimationController.Pause(entity);
            else
                AnimationController.Resume(entity);
        });

        ImGui.SameLine();

        // Frame scrubber
        var currentFrame = component.CurrentFrameIndex;
        var maxFrame = clip.Frames.Length - 1;
        var scrubberWidth = ImGui.GetContentRegionAvail().X;

        ImGui.SetNextItemWidth(scrubberWidth);
        if (ImGui.SliderInt("##FrameScrubber", ref currentFrame, 0, maxFrame,
                $"Frame: {currentFrame} / {clip.Frames.Length}"))
        {
            AnimationController.SetFrame(entity, currentFrame);
        }

        // Time info
        var currentTime = currentFrame / clip.Fps;
        ImGui.Text($"Time: {currentTime:F2}s / {clip.Duration:F2}s");
    }

    private void DrawFrameInfo(AnimationComponent component)
    {
        var clip = component.Asset!.GetClip(component.CurrentClipName);
        if (clip == null || component.CurrentFrameIndex >= clip.Frames.Length)
            return;

        var frame = clip.Frames[component.CurrentFrameIndex];

        ImGui.Text("Frame Info:");
        ImGui.Indent();

        // Rect
        ImGui.Text($"Rect: [{frame.Rect.X}, {frame.Rect.Y}, {frame.Rect.Width}, {frame.Rect.Height}]");

        // Pivot
        ImGui.Text($"Pivot: [{frame.Pivot.X:F2}, {frame.Pivot.Y:F2}]");

        // Events
        if (frame.Events is { Length: > 0 })
        {
            ImGui.Text("Events: " + string.Join(", ", frame.Events));
        }
        else
        {
            ImGui.TextDisabled("Events: (none)");
        }

        ImGui.Unindent();
    }

    private void DrawClipList(Entity entity, AnimationComponent component)
    {
        var asset = component.Asset!;

        ImGui.Text("Available Clips:");
        ImGui.Indent();

        foreach (var clip in asset.Clips)
        {
            var clipName = clip.Name;
            ImGui.BulletText($"{clipName}  ({clip.Frames.Length} frames, {clip.Fps} fps)");
            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - EditorUIConstants.StandardButtonWidth -
                                EditorUIConstants.StandardPadding);

            ButtonDrawer.DrawButton($"Preview##{clipName}", EditorUIConstants.StandardButtonWidth, 0, () =>
            {
                // Play clip once without looping
                AnimationController.Play(entity, clipName, forceRestart: true);
                component.Loop = false;
            });
        }

        ImGui.Unindent();
    }

    private void DrawActionButtons(Entity entity, AnimationComponent component)
    {
        ButtonDrawer.DrawButton("Open Timeline Editor", EditorUIConstants.WideButtonWidth, 0, () =>
        {
            if (timelineWindow != null)
            {
                timelineWindow.SetEntity(entity);
            }
        });

        ImGui.SameLine();

        var showDebug = component.ShowDebugInfo;
        if (ImGui.Checkbox("Show Debug", ref showDebug))
        {
            component.ShowDebugInfo = showDebug;
        }
    }
}