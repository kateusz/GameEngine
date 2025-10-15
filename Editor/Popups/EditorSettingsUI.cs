using System.Numerics;
using Engine.Renderer.Cameras;
using ImGuiNET;

namespace Editor.Panels;

#pragma warning disable CS0618 // Type or member is obsolete - Editor still uses legacy camera system until Phase 3 migration
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

        ImGui.End();
    }
}
#pragma warning restore CS0618 // Type or member is obsolete

/// <summary>
/// Simple class to hold editor-wide settings.
/// </summary>
public class EditorSettings
{
    public Vector4 BackgroundColor = new Vector4(0.91f, 0.91f, 0.91f, 1.0f); // normalized color
}