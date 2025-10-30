using System.Numerics;
using System.Runtime.InteropServices;
using ECS;
using Editor.Panels.Elements;
using ImGuiNET;
using Engine.Scene.Components;
using Engine.Animation;
using Editor.UI;
using Serilog;

namespace Editor.Panels.ComponentEditors;

/// <summary>
/// Inspector editor for AnimationComponent.
/// Provides UI for asset selection, clip control, playback settings, and frame scrubbing.
/// </summary>
public class AnimationComponentEditor : IComponentEditor
{
    private static readonly ILogger Logger = Log.ForContext<AnimationComponentEditor>();
    
    private readonly AnimationAssetManager _animationAssetManager;

    public AnimationComponentEditor(AnimationAssetManager animationAssetManager)
    {
        _animationAssetManager = animationAssetManager;
    }

    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<AnimationComponent>("Animation", e, entity =>
        {
            var component = entity.GetComponent<AnimationComponent>();

            DrawAssetPath(component);
            ImGui.Spacing();

            if (component.Asset != null)
            {
                DrawClipSelector(component);
                ImGui.Spacing();

                DrawPlaybackControls(component);
                ImGui.Spacing();

                DrawTimeline(component);
                ImGui.Spacing();

                DrawFrameInfo(component);
                ImGui.Spacing();

                DrawClipList(component);
                ImGui.Spacing();

                DrawActionButtons(component);
            }
            else
            {
                ImGui.TextColored(EditorUIConstants.WarningColor, "No animation asset loaded");
            }

            ImGui.PopID();
        });
    }

    private void DrawAssetPath(AnimationComponent component)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - EditorUIConstants.StandardButtonWidth -
                               EditorUIConstants.StandardPadding);

        ImGui.Text($"Asset Path: {component.AssetPath}");

        UIPropertyRenderer.DrawPropertyRow("Animation", () =>
        {
            if (ImGui.Button("Animation", new Vector2(-1, 0.0f)))
            {
                // Optional: Handle button click logic if needed
            }

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("CONTENT_BROWSER_ITEM");
                unsafe
                {
                    if (payload.NativePtr != null)
                    {
                        // Get dropped asset path
                        var droppedPath = Marshal.PtrToStringUni(payload.Data);

                        if (!string.IsNullOrWhiteSpace(droppedPath) &&
                            droppedPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                        {
                            var animation = _animationAssetManager.LoadAsset(droppedPath);
                            component.AssetPath = droppedPath;
                            component.Asset = animation;
                        }
                    }
                }

                ImGui.EndDragDropTarget();
            }
        });

        // Show load status
        if (component.Asset == null && !string.IsNullOrEmpty(component.AssetPath))
        {
            ImGui.TextColored(EditorUIConstants.ErrorColor, "Failed to load");
        }
    }

    private void DrawClipSelector(AnimationComponent component)
    {
        var asset = component.Asset!;
        var currentClip = asset.GetClip(component.CurrentClipName);

        ImGui.Text("Current Clip:");
        ImGui.SameLine();

        // Clip dropdown
        ImGui.SetNextItemWidth(EditorUIConstants.WideColumnWidth);
        if (ImGui.BeginCombo("##ClipSelector", component.CurrentClipName))
        {
            foreach (var clip in asset.Clips)
            {
                var clipName = clip.Name;
                var isSelected = clipName == component.CurrentClipName;
                if (ImGui.Selectable(clipName, isSelected))
                {
                    // TODO: how should it be done?
                    // component.Play(clipName);
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        // Clip info
        if (currentClip != null)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"({currentClip.Frames.Length} frames, {currentClip.Duration:F2}s)");
        }
    }

    private void DrawPlaybackControls(AnimationComponent component)
    {
        ImGui.Text("Playback:");
        ImGui.SameLine();

        // Playing checkbox
        var isPlaying = component.IsPlaying;
        if (ImGui.Checkbox("Playing", ref isPlaying))
        {
            // TODO
            // if (isPlaying)
            //     component.Resume();
            // else
            //     component.Pause();
        }

        ImGui.SameLine();

        // Loop checkbox
        bool loop = component.Loop;
        if (ImGui.Checkbox("Loop", ref loop))
        {
            component.Loop = loop;
        }

        ImGui.SameLine();

        // Speed slider
        ImGui.Text("Speed:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(EditorUIConstants.FilterInputWidth);
        float speed = component.PlaybackSpeed;
        if (ImGui.SliderFloat("##Speed", ref speed, 0.1f, 3.0f, "%.1fx"))
        {
            // TODO
            //component.SetSpeed(speed);
        }
    }

    private void DrawTimeline(AnimationComponent component)
    {
        var clip = component.Asset!.GetClip(component.CurrentClipName);
        if (clip == null) return;

        ImGui.Text("Timeline:");

        // Play button
        if (ImGui.Button(component.IsPlaying ? "⏸" : "▶",
                new Vector2(EditorUIConstants.SmallButtonSize, EditorUIConstants.SmallButtonSize)))
        {
            // TODO
            // if (component.IsPlaying)
            //     component.Pause();
            // else
            //     component.Resume();
        }

        ImGui.SameLine();

        // Frame scrubber
        int currentFrame = component.CurrentFrameIndex;
        int maxFrame = clip.Frames.Length - 1;
        float scrubberWidth = ImGui.GetContentRegionAvail().X;

        ImGui.SetNextItemWidth(scrubberWidth);
        if (ImGui.SliderInt("##FrameScrubber", ref currentFrame, 0, maxFrame,
                $"Frame: {currentFrame} / {clip.Frames.Length}"))
        {
            // TODO
            //component.SetFrame(currentFrame);
        }

        // Time info
        float currentTime = currentFrame / clip.Fps;
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
        ImGui.SameLine(EditorUIConstants.WideColumnWidth);

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

    private void DrawClipList(AnimationComponent component)
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

            if (ImGui.Button($"Preview##{clipName}", new Vector2(EditorUIConstants.StandardButtonWidth, 0)))
            {
                // TODO
                // // Play clip once and return to previous
                // string previousClip = component.CurrentClipName;
                // component.Play(clipName);
                // component.Loop = false;

                // TODO: Subscribe to AnimationCompleteEvent to return to previous clip
            }
        }

        ImGui.Unindent();
    }

    private void DrawActionButtons(AnimationComponent component)
    {
        if (ImGui.Button("Open Timeline Editor", new Vector2(EditorUIConstants.WideButtonWidth, 0)))
        {
            // TODO: Open AnimationTimelineWindow (Phase 4)
            Logger.Information("Timeline editor not yet implemented (Phase 4)");
        }

        ImGui.SameLine();

        bool showDebug = component.ShowDebugInfo;
        if (ImGui.Checkbox("Show Debug", ref showDebug))
        {
            component.ShowDebugInfo = showDebug;
        }
    }
}