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
        if (!_open) return;

        ImGui.Begin("Editor Settings", ref _open, ImGuiWindowFlags.AlwaysAutoResize);

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

        bool showPhysics = DebugSettings.Instance.ShowPhysicsDebug;
        if (ImGui.Checkbox("Show Physics Debug", ref showPhysics))
            DebugSettings.Instance.ShowPhysicsDebug = showPhysics;

        bool showColliders = DebugSettings.Instance.ShowColliderBounds;
        if (ImGui.Checkbox("Show Collider Bounds", ref showColliders))
            DebugSettings.Instance.ShowColliderBounds = showColliders;

        bool showFps = DebugSettings.Instance.ShowFPS;
        if (ImGui.Checkbox("Show FPS Counter", ref showFps))
            DebugSettings.Instance.ShowFPS = showFps;

        ImGui.End();
    }
}

/// <summary>
/// Simple class to hold editor-wide settings.
/// </summary>
public class EditorSettings
{
    public Vector4 BackgroundColor = new Vector4(0.91f, 0.91f, 0.91f, 1.0f); // normalized color
}