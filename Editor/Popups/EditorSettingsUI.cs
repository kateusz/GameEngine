using System.Numerics;
using Editor.UI;
using Engine.Core;
using Engine.Renderer.Cameras;
using ImGuiNET;

namespace Editor.Panels;

public class EditorSettingsUI
{
    private bool _open = false;

    private readonly OrthographicCameraController _cameraController;
    public readonly EditorSettings Settings;

    public EditorSettingsUI(OrthographicCameraController cameraController, EditorSettings settings)
    {
        _cameraController = cameraController;
        Settings = settings;
    }

    public void Show() => _open = true;

    public void Render()
    {
        // Open the popup when _open is set to true
        if (_open)
            ImGui.OpenPopup("Editor Settings");

        // Center the popup on first appearance
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        // Use BeginPopupModal for proper popup behavior
        if (!ImGui.BeginPopupModal("Editor Settings", ref _open,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
            return;

        // --- Background Color ---
        ImGui.Text("Editor Background Color");
        ImGui.ColorEdit4("Background Color", ref Settings.BackgroundColor);

        ImGui.Separator();

        // --- Camera Settings ---
        ImGui.Text("Camera Settings");

        var camPos = _cameraController.Camera.Position;
        if (ImGui.DragFloat3("Camera Position", ref camPos, 0.1f))
        {
            _cameraController.Camera.SetPosition(camPos);
        }

        var camRot = _cameraController.Camera.Rotation;
        if (ImGui.DragFloat("Camera Rotation", ref camRot, 1.0f))
        {
            _cameraController.Camera.SetRotation(camRot);
        }

        ImGui.Separator();

        // --- Debug Visualization Settings ---
        ImGui.SeparatorText("Debug Visualization");

        bool showColliders = DebugSettings.Instance.ShowColliderBounds;
        if (ImGui.Checkbox("Show Collider Bounds", ref showColliders))
            DebugSettings.Instance.ShowColliderBounds = showColliders;

        bool showFps = DebugSettings.Instance.ShowFPS;
        if (ImGui.Checkbox("Show FPS Counter", ref showFps))
            DebugSettings.Instance.ShowFPS = showFps;

        ImGui.EndPopup();
    }
}

/// <summary>
/// Simple class to hold editor-wide settings.
/// </summary>
public class EditorSettings
{
    public Vector4 BackgroundColor = new Vector4(0.91f, 0.91f, 0.91f, 1.0f); // normalized color
}