using System.Numerics;
using ECS;
using ImGuiNET;
using Engine.Scene.Components;
using Editor.UI;

namespace Editor.Windows
{
    /// <summary>
    /// Advanced animation timeline editor window.
    /// Provides visual authoring tools for animators with frame-by-frame control.
    /// </summary>
    public class AnimationTimelineWindow
    {
        private bool _isOpen = false;
        private Entity? _selectedEntity;
        private AnimationComponent? _component;

        // Independent playback state (decoupled from scene play mode)
        private bool _previewPlaying = false;
        private float _previewSpeed = 1.0f;
        private int _selectedFrameIndex = 0;

        // UI state
        private float _timelineScrollX = 0.0f;
        private const float FrameBoxWidth = 60.0f;
        private const float FrameBoxHeight = 60.0f;
        private const float TimelineHeight = 120.0f;

        public void SetEntity(Entity entity)
        {
            _selectedEntity = entity;
            _component = entity.GetComponent<AnimationComponent>();
            _isOpen = true;
        }

        public void OnImGuiRender()
        {
            if (!_isOpen)
                return;

            ImGui.SetNextWindowSize(new Vector2(900, 600), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("Animation Timeline", ref _isOpen, ImGuiWindowFlags.MenuBar))
            {
                if (_component == null || _component.Asset == null)
                {
                    ImGui.TextColored(EditorUIConstants.WarningColor, "No animation component selected");
                    ImGui.Text("Select an entity with AnimationComponent to edit animations");
                }
                else
                {
                    DrawMenuBar();
                    DrawEntityInfo();
                    ImGui.Separator();
                    DrawClipSelector();
                    ImGui.Separator();
                    DrawPlaybackControls();
                    ImGui.Separator();
                    DrawTimeline();
                    ImGui.Separator();
                    DrawFrameDetails();
                    ImGui.Separator();
                    DrawStatistics();
                }
            }
            ImGui.End();
        }

        private static void DrawMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("View"))
                {
                    ImGui.MenuItem("Show Event Markers", "", true);
                    ImGui.MenuItem("Show Frame Numbers", "", true);
                    ImGui.MenuItem("Show Grid", "", true);
                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }
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
                    bool isSelected = clipName == _component.CurrentClipName;
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
                ImGui.TextDisabled($"({currentClip.Frames.Length} frames, {currentClip.Fps} fps, {currentClip.Duration:F2}s duration)");
            }
        }

        private void DrawPlaybackControls()
        {
            // Play/Pause button
            if (ImGui.Button(_previewPlaying ? "|| Pause" : "> Play", new Vector2(EditorUIConstants.StandardButtonWidth, 0)))
            {
                _previewPlaying = !_previewPlaying;
            }

            ImGui.SameLine();

            // Stop button
            if (ImGui.Button("[] Stop", new Vector2(EditorUIConstants.StandardButtonWidth, 0)))
            {
                _previewPlaying = false;
                _selectedFrameIndex = 0;
            }

            ImGui.SameLine();

            // Loop toggle
            var loop = _component!.Loop;
            if (ImGui.Button(loop ? "Loop: On" : "Loop: Off", new Vector2(EditorUIConstants.StandardButtonWidth, 0)))
            {
                _component.Loop = !loop;
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
            if (clip == null) return;

            ImGui.Text("Timeline:");

            // Timeline container with scrolling
            ImGui.BeginChild("TimelineScroll", new Vector2(0, TimelineHeight));

            var drawList = ImGui.GetWindowDrawList();
            var cursorPos = ImGui.GetCursorScreenPos();
            var atlas = _component!.Asset!.Atlas;

            // Draw frame boxes
            for (var i = 0; i < clip.Frames.Length; i++)
            {
                var framePos = new Vector2(cursorPos.X + i * (FrameBoxWidth + 10), cursorPos.Y + 30);
                var frameSize = new Vector2(FrameBoxWidth, FrameBoxHeight);
                var frame = clip.Frames[i];

                // Frame box background
                uint frameColor = i == _selectedFrameIndex
                    ? ImGui.GetColorU32(new Vector4(0.3f, 0.5f, 0.8f, 1.0f))
                    : ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1.0f));

                drawList.AddRectFilled(framePos, framePos + frameSize, frameColor);
                drawList.AddRect(framePos, framePos + frameSize, ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));

                // Render frame thumbnail
                if (atlas != null)
                {
                    var texturePointer = new IntPtr(atlas.GetRendererId());
                    var uvMin = frame.TexCoords[0]; // Bottom-left
                    var uvMax = frame.TexCoords[2]; // Top-right
                    
                    // Calculate thumbnail size maintaining aspect ratio (with padding for frame number)
                    
                    float maxSize = Math.Min(FrameBoxWidth - 4, FrameBoxHeight);
                    float aspectRatio = (float)frame.Rect.Width / frame.Rect.Height;
                    Vector2 thumbnailSize;
                    
                    thumbnailSize = aspectRatio > 1.0f ? new Vector2(maxSize, maxSize / aspectRatio) : new Vector2(maxSize * aspectRatio, maxSize);
                    
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
            int currentFrame = _component.CurrentFrameIndex;
            var playheadPos = new Vector2(cursorPos.X + currentFrame * (FrameBoxWidth + 10) + FrameBoxWidth / 2, cursorPos.Y);
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

            ImGui.Columns(2, "FrameDetailsColumns", false);
            ImGui.SetColumnWidth(0, 150);

            // Left column: Frame preview
            var atlas = _component!.Asset!.Atlas;
            if (atlas != null)
            {
                var texturePointer = new IntPtr(atlas.GetRendererId());
                
                // Use the pre-calculated UV coordinates from the frame
                // TexCoords: [0]=bottom-left, [1]=bottom-right, [2]=top-right, [3]=top-left
                var uvMin = frame.TexCoords[0]; // Bottom-left
                var uvMax = frame.TexCoords[2]; // Top-right
                
                // Calculate preview size maintaining aspect ratio
                const float maxPreviewSize = 128.0f;
                float aspectRatio = (float)frame.Rect.Width / frame.Rect.Height;
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

            ImGui.NextColumn();

            // Right column: Frame metadata
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

            ImGui.Columns(1);
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
            if (_component.Asset.Atlas != null)
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
            float frameDuration = 1.0f / clip.Fps;
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
}