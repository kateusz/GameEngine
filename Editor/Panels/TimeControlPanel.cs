using System.Numerics;
using Editor.UI;
using Engine.Core;
using ImGuiNET;

namespace Editor.Panels;

/// <summary>
/// Editor panel for controlling game simulation time.
/// Provides pause/resume, frame stepping, and time scale controls for debugging and gameplay testing.
/// </summary>
public class TimeControlPanel
{
    private readonly IApplication _application;

    /// <summary>
    /// Gets or sets whether the time control panel is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    public TimeControlPanel(IApplication application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    /// <summary>
    /// Renders the time control panel UI.
    /// </summary>
    public void Draw()
    {
        if (!IsVisible)
            return;

        var isVisible = IsVisible;
        ImGui.Begin("Time Control", ref isVisible);
        IsVisible = isVisible;

        // Pause/Resume button
        var isPaused = _application.IsPaused;
        var buttonLabel = isPaused ? "▶ Resume" : "⏸ Pause";
        var buttonColor = isPaused ? EditorUIConstants.SuccessColor : EditorUIConstants.WarningColor;

        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
        if (ImGui.Button(buttonLabel, new Vector2(EditorUIConstants.WideButtonWidth, EditorUIConstants.StandardButtonHeight)))
        {
            _application.IsPaused = !_application.IsPaused;
        }
        ImGui.PopStyleColor();

        // Frame step button (only visible when paused)
        if (isPaused)
        {
            ImGui.SameLine();
            if (ImGui.Button("⏭ Step Frame", new Vector2(EditorUIConstants.WideButtonWidth, EditorUIConstants.StandardButtonHeight)))
            {
                _application.StepSingleFrame();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Advance simulation by 1/60th second");
                ImGui.EndTooltip();
            }
        }

        ImGui.Separator();

        // Time scale section
        ImGui.Text("Time Scale");

        var timeScale = _application.TimeScale;

        // Slider for continuous control
        if (ImGui.SliderFloat("##TimeScale", ref timeScale, 0.0f, 2.0f, $"{timeScale:F2}x"))
        {
            _application.TimeScale = timeScale;
        }

        // Quick preset buttons
        ImGui.Text("Presets:");

        if (ImGui.Button("0.25x", new Vector2(EditorUIConstants.SmallButtonSize * 2, EditorUIConstants.StandardButtonHeight)))
        {
            _application.TimeScale = 0.25f;
        }

        ImGui.SameLine();
        if (ImGui.Button("0.5x", new Vector2(EditorUIConstants.SmallButtonSize * 2, EditorUIConstants.StandardButtonHeight)))
        {
            _application.TimeScale = 0.5f;
        }

        ImGui.SameLine();
        if (ImGui.Button("1.0x", new Vector2(EditorUIConstants.SmallButtonSize * 2, EditorUIConstants.StandardButtonHeight)))
        {
            _application.TimeScale = 1.0f;
        }

        ImGui.SameLine();
        if (ImGui.Button("2.0x", new Vector2(EditorUIConstants.SmallButtonSize * 2, EditorUIConstants.StandardButtonHeight)))
        {
            _application.TimeScale = 2.0f;
        }

        ImGui.Separator();

        // Status information
        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.InfoColor);
        ImGui.Text($"Status: {(isPaused ? "Paused" : "Running")}");
        ImGui.Text($"Current Speed: {_application.TimeScale:F2}x");

        if (isPaused)
        {
            ImGui.Text("Press Step Frame to advance");
        }
        ImGui.PopStyleColor();

        ImGui.End();
    }
}
