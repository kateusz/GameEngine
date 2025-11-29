using System.Numerics;
using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels;

/// <summary>
/// Advanced animation timeline editor window.
/// Provides visual authoring tools for animators with frame-by-frame control.
/// </summary>
public class AnimationTimelinePanel
{
    private bool _isOpen;
    private bool _hasBeenDockedOnce;
    private Entity? _selectedEntity;
    private AnimationComponent? _component;

    // Independent playback state (decoupled from scene play mode)
    private bool _previewPlaying;
    private float _previewSpeed = 1.0f;
    private int _selectedFrameIndex;

    // UI state
    private const float FrameBoxWidth = 120.0f;
    private const float FrameBoxHeight = 120.0f;
    private const float TimelineHeight = 150.0f;

    public void SetEntity(Entity entity)
    {
        _selectedEntity = entity;
        _component = entity.GetComponent<AnimationComponent>();
        _isOpen = true;
    }

    public void OnImGuiRender(uint viewportDockId = 0)
    {
        if (!_isOpen)
            return;

        var wasOpen = _isOpen;

        // Dock to Viewport on first open
        if (!_hasBeenDockedOnce && viewportDockId != 0)
        {
            ImGui.SetNextWindowDockID(viewportDockId);
            _hasBeenDockedOnce = true;
        }

        if (ImGui.Begin("Animation Timeline", ref _isOpen, ImGuiWindowFlags.MenuBar))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.LargePadding));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));

            if (_component?.Asset == null)
            {
                ImGui.Spacing();
                ImGui.Indent(EditorUIConstants.LargePadding);
                TextDrawer.DrawWarningText("No animation component selected");
                ImGui.Text("Select an entity with AnimationComponent to edit animations");
                ImGui.Unindent(EditorUIConstants.LargePadding);
            }
            else
            {
                ImGui.Spacing();
                DrawEntityInfo();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                DrawClipSelector();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                DrawPlaybackControls();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                DrawTimeline();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                DrawFrameDetails();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                DrawStatistics();
                ImGui.Spacing();
            }

            ImGui.PopStyleVar(2);
        }

        ImGui.End();

        // If window was just closed via X button, reset state
        if (wasOpen && !_isOpen)
        {
            ResetState();
        }
    }

    private void ResetState()
    {
        _previewPlaying = false;
        _selectedFrameIndex = 0;
        _selectedEntity = null;
        _component = null;
        _hasBeenDockedOnce = false;
    }
        
    private void DrawEntityInfo()
    {
        ImGui.Text($"Entity: {_selectedEntity!.Name}");
        ImGui.SameLine(250);
        ImGui.Text($"Asset: {_component!.AssetPath}");
    }

    private void DrawClipSelector()
    {
        var asset = _component!.Asset!;
        var currentClip = asset.GetClip(_component.CurrentClipName);

        ImGui.Text("Clip:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(EditorUIConstants.WideColumnWidth);
        if (ImGui.BeginCombo("##ClipSelector", _component.CurrentClipName))
        {
            foreach (var clipName in asset.Clips.Select(x => x.Name))
            {
                var isSelected = clipName == _component.CurrentClipName;
                if (ImGui.Selectable(clipName, isSelected))
                {
                    _component.CurrentClipName = clipName;
                    _selectedFrameIndex = 0;
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        if (currentClip != null)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(
                $"({currentClip.Frames.Length} frames, {currentClip.Fps} fps, {currentClip.Duration:F2}s duration)");
        }
    }

    private void DrawPlaybackControls()
    {
        // Play/Pause button
        ButtonDrawer.DrawToggleButton("|| Pause", "> Play", ref _previewPlaying);

        ImGui.SameLine();

        // Stop button
        ButtonDrawer.DrawModalButton("[] Stop", onClick: () =>
        {
            _previewPlaying = false;
            _selectedFrameIndex = 0;
        });

        ImGui.SameLine();

        // Loop toggle
        var loop = _component!.Loop;
        if (ButtonDrawer.DrawToggleButton("Loop: On", "Loop: Off", ref loop))
        {
            _component.Loop = loop;
        }

        ImGui.SameLine();

        // Speed slider
        ImGui.Text("Speed:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(EditorUIConstants.FilterInputWidth);
        ImGui.SliderFloat("##PreviewSpeed", ref _previewSpeed, 0.1f, 3.0f, "%.1fx");
    }

    private void DrawTimeline()
    {
        var clip = _component!.Asset!.GetClip(_component.CurrentClipName);
        if (clip == null)
        {
            return;
        }

        ImGui.Text("Timeline:");
        ImGui.Spacing();

        // Timeline container with scrolling
        ImGui.BeginChild("TimelineScroll", new Vector2(0, TimelineHeight), ImGuiChildFlags.Border);
        ImGui.Dummy(new Vector2(0, EditorUIConstants.LargePadding)); // Top padding

        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();
        var atlas = _component!.Asset!.Atlas;

        // Add left padding
        cursorPos.X += EditorUIConstants.LargePadding;

        // Draw frame boxes
        for (var i = 0; i < clip.Frames.Length; i++)
        {
            var framePos = new Vector2(cursorPos.X + i * (FrameBoxWidth + EditorUIConstants.LargePadding * 2), cursorPos.Y + 30);
            var frameSize = new Vector2(FrameBoxWidth, FrameBoxHeight);
            var frame = clip.Frames[i];

            // Frame box background
            var frameColor = i == _selectedFrameIndex
                ? ImGui.GetColorU32(new Vector4(0.3f, 0.5f, 0.8f, 1.0f))
                : ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1.0f));

            drawList.AddRectFilled(framePos, framePos + frameSize, frameColor);
            drawList.AddRect(framePos, framePos + frameSize,
                ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));

            // Render frame thumbnail
            if (atlas != null)
            {
                var texturePointer = new IntPtr(atlas.GetRendererId());

                // Swap Y coordinates to fix upside-down rendering
                var uvMin = new Vector2(frame.TexCoords[0].X, frame.TexCoords[2].Y); // Bottom-left X, Top-right Y
                var uvMax = new Vector2(frame.TexCoords[2].X, frame.TexCoords[0].Y); // Top-right X, Bottom-left Y

                // Calculate thumbnail size maintaining aspect ratio (with padding for frame number)

                var maxSize = Math.Min(FrameBoxWidth - 4, FrameBoxHeight);
                var aspectRatio = (float)frame.Rect.Width / frame.Rect.Height;
                Vector2 thumbnailSize;

                thumbnailSize = aspectRatio > 1.0f
                    ? new Vector2(maxSize, maxSize / aspectRatio)
                    : new Vector2(maxSize * aspectRatio, maxSize);

                // Center the thumbnail in the frame box (below frame number)
                var thumbnailPos = framePos + new Vector2(
                    (FrameBoxWidth - thumbnailSize.X) / 2,
                    (FrameBoxHeight - thumbnailSize.Y) / 2
                );

                ImGui.SetCursorScreenPos(thumbnailPos);
                ImGui.Image(texturePointer, thumbnailSize, uvMin, uvMax);
            }

            // Event markers
            if (frame.Events.Length > 0)
            {
                var eventPos = new Vector2(framePos.X + FrameBoxWidth / 2 - 10, framePos.Y - 25);
                drawList.AddText(eventPos, ImGui.GetColorU32(EditorUIConstants.WarningColor), "[E]");
            }

            // Clickable area (overlay on top of everything)
            ImGui.SetCursorScreenPos(framePos);
            ImGui.InvisibleButton($"Frame_{i}", frameSize);

            if (ImGui.IsItemClicked())
            {
                _selectedFrameIndex = i;
                _component.CurrentFrameIndex = i;
            }

            // Tooltip
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"Frame {i}");
                ImGui.Text($"Rect: [{frame.Rect.X}, {frame.Rect.Y}, {frame.Rect.Width}, {frame.Rect.Height}]");
                if (frame.Events.Length > 0)
                    ImGui.Text($"Events: {string.Join(", ", frame.Events)}");
                ImGui.EndTooltip();
            }
        }

        // Playhead indicator
        var currentFrame = _component.CurrentFrameIndex;
        var playheadPos = cursorPos with { X = cursorPos.X + currentFrame * (FrameBoxWidth + EditorUIConstants.LargePadding * 2) + FrameBoxWidth / 2 };
        drawList.AddLine(playheadPos, playheadPos + new Vector2(0, TimelineHeight - 20),
            ImGui.GetColorU32(EditorUIConstants.ErrorColor), 3.0f);
        drawList.AddTriangleFilled(
            playheadPos + new Vector2(-5, -10),
            playheadPos + new Vector2(5, -10),
            playheadPos,
            ImGui.GetColorU32(EditorUIConstants.ErrorColor));

        ImGui.EndChild();
    }

    private void DrawFrameDetails()
    {
        var clip = _component!.Asset!.GetClip(_component.CurrentClipName);
        if (clip == null || _selectedFrameIndex >= clip.Frames.Length)
            return;

        var frame = clip.Frames[_selectedFrameIndex];

        ImGui.Text($"Frame Details: Frame {_selectedFrameIndex}");
        ImGui.Spacing();

        ImGui.BeginChild("FrameDetailsContent", new Vector2(0, 200), ImGuiChildFlags.Border);
        ImGui.Dummy(new Vector2(0, EditorUIConstants.SmallPadding)); // Top padding

        ImGui.Columns(2, "FrameDetailsColumns", false);
        ImGui.SetColumnWidth(0, 170);

        // Left column: Frame preview
        ImGui.Indent(EditorUIConstants.LargePadding);
        var atlas = _component!.Asset!.Atlas;
        if (atlas != null)
        {
            var texturePointer = new IntPtr(atlas.GetRendererId());

            // Use the pre-calculated UV coordinates from the frame
            // TexCoords: [0]=bottom-left, [1]=bottom-right, [2]=top-right, [3]=top-left
            // Swap Y coordinates to fix upside-down rendering
            var uvMin = new Vector2(frame.TexCoords[0].X, frame.TexCoords[2].Y); // Bottom-left X, Top-right Y
            var uvMax = new Vector2(frame.TexCoords[2].X, frame.TexCoords[0].Y); // Top-right X, Bottom-left Y

            // Calculate preview size maintaining aspect ratio
            const float maxPreviewSize = 128.0f;
            var aspectRatio = (float)frame.Rect.Width / frame.Rect.Height;
            Vector2 previewSize;

            if (aspectRatio > 1.0f)
            {
                // Wider than tall
                previewSize = new Vector2(maxPreviewSize, maxPreviewSize / aspectRatio);
            }
            else
            {
                // Taller than wide
                previewSize = new Vector2(maxPreviewSize * aspectRatio, maxPreviewSize);
            }

            // Render the frame texture
            ImGui.Image(texturePointer, previewSize, uvMin, uvMax);
        }
        else
        {
            ImGui.TextDisabled("[No Atlas]");
        }
        ImGui.Unindent(EditorUIConstants.LargePadding);

        ImGui.NextColumn();

        // Right column: Frame metadata
        ImGui.Indent(EditorUIConstants.LargePadding);
        ImGui.Text($"Rect: [{frame.Rect.X}, {frame.Rect.Y}, {frame.Rect.Width}, {frame.Rect.Height}]");
        ImGui.Text($"Pivot: [{frame.Pivot.X:F2}, {frame.Pivot.Y:F2}]");

        ImGui.Text($"Flip: H[{(frame.Flip?.X > 0.5f ? "✓" : "✗")}] V[{(frame.Flip?.Y > 0.5f ? "✓" : "✗")}]");
        ImGui.Text($"Rotation: {frame.Rotation:F1}°");
        ImGui.Text($"Scale: [{frame.Scale.X:F2}, {frame.Scale.Y:F2}]");

        if (frame.Events != null && frame.Events.Length > 0)
        {
            ImGui.Text($"Events: [{string.Join(", ", frame.Events)}]");
        }
        else
        {
            ImGui.TextDisabled("Events: (none)");
        }
        ImGui.Unindent(EditorUIConstants.LargePadding);

        ImGui.Columns(1);
        ImGui.Dummy(new Vector2(0, EditorUIConstants.SmallPadding)); // Bottom padding
        ImGui.EndChild();
    }

    private void DrawStatistics()
    {
        var clip = _component!.Asset!.GetClip(_component.CurrentClipName);
        if (clip == null) return;

        ImGui.Text("Statistics:");
        ImGui.Indent();

        ImGui.Text($"Total Frames: {clip.Frames.Length}");
        ImGui.SameLine(200);
        ImGui.Text($"Duration: {clip.Duration:F2}s");
        ImGui.SameLine(350);

        // Approximate memory usage
        long memoryUsage = clip.Frames.Length * 256; // ~256 bytes per frame
        if (_component.Asset?.Atlas != null)
        {
            memoryUsage += _component.Asset.Atlas.Width * _component.Asset.Atlas.Height * 4;
        }

        ImGui.Text($"Memory: {memoryUsage / 1024.0f:F1} KB");

        ImGui.Unindent();
    }

    // Update preview playback (called from editor layer update)
    public void Update(float deltaTime)
    {
        if (!_isOpen || !_previewPlaying || _component == null)
            return;

        var clip = _component.Asset?.GetClip(_component.CurrentClipName);
        if (clip == null) return;

        // Advance preview frame independently
        var frameDuration = 1.0f / clip.Fps;
        _component.FrameTimer += deltaTime * _previewSpeed;

        if (_component.FrameTimer >= frameDuration)
        {
            _component.FrameTimer -= frameDuration;
            _selectedFrameIndex++;

            if (_selectedFrameIndex >= clip.Frames.Length)
            {
                if (_component.Loop)
                    _selectedFrameIndex = 0;
                else
                {
                    _selectedFrameIndex = clip.Frames.Length - 1;
                    _previewPlaying = false;
                }
            }

            _component.CurrentFrameIndex = _selectedFrameIndex;
        }
    }
}