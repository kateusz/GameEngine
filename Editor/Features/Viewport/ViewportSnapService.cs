using System.Diagnostics;
using System.Numerics;
using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.UI.Constants;
using Math;

namespace Editor.Features.Viewport;

/// <summary>
/// DryIoc singleton snap policy. Depends only on <see cref="IEditorPreferences"/> — no tool/toolbar reverse deps.
/// </summary>
public sealed class ViewportSnapService(IEditorPreferences preferences) : IViewportSnapService
{
    public bool Enabled
    {
        get => preferences.SnapEnabled;
        set
        {
            if (preferences.SnapEnabled == value)
                return;
            preferences.SnapEnabled = value;
            preferences.Save();
        }
    }

    public float GridStep
    {
        get => preferences.GridStep;
        set
        {
            if (preferences.GridStep == value)
                return;
            preferences.GridStep = value;
            preferences.Save();
        }
    }

    public bool ShouldSnap(bool bypassHeld) => preferences.SnapEnabled && !bypassHeld;

    public Vector3 SnapWorldPosition(Vector3 worldPosition, bool snapX, bool snapY)
        => SnapMath.QuantizeWorldXY(worldPosition, preferences.GridStep, snapX, snapY);

    /// <summary>Group 3 verification (3C / D2A). Host once from EditorLifecycle in Debug.</summary>
    [Conditional("DEBUG")]
    public static void SelfCheck()
    {
        var fresh = new EditorPreferences();
        Debug.Assert(fresh.SnapEnabled == false, "ViewportSnap: default SnapEnabled must be false");
        Debug.Assert(fresh.GridStep == 1.0f, "ViewportSnap: default GridStep must be 1.0");

        // Shared UI clamp (toolbar + Settings)
        Debug.Assert(EditorUIConstants.SnapGridStepMin > 0f);
        Debug.Assert(SceneToolbar.ClampGridStep(0f) == EditorUIConstants.SnapGridStepMin);
        Debug.Assert(SceneToolbar.ClampGridStep(0.5f) == 0.5f);

        var recording = new RecordingPreferences { SnapEnabled = true, GridStep = 1.0f };
        var service = new ViewportSnapService(recording);

        Debug.Assert(service.ShouldSnap(bypassHeld: true) == false);
        Debug.Assert(service.ShouldSnap(bypassHeld: false) == true);

        recording.SnapEnabled = false;
        Debug.Assert(service.ShouldSnap(bypassHeld: false) == false);

        recording.SaveCount = 0;
        service.GridStep = 0.5f;
        Debug.Assert(recording.GridStep == 0.5f && recording.SaveCount == 1);

        recording.SaveCount = 0;
        _ = service.GridStep;
        _ = service.ShouldSnap(false);
        _ = service.SnapWorldPosition(new Vector3(1.4f, 2.6f, 5f), snapX: true, snapY: true);
        Debug.Assert(recording.SaveCount == 0, "ViewportSnap: hot path must not Save");

        recording.SaveCount = 0;
        service.Enabled = true;
        Debug.Assert(recording.SnapEnabled == true && recording.SaveCount == 1);

        recording.GridStep = 1.0f;
        var snapped = service.SnapWorldPosition(new Vector3(1.4f, 2.6f, 5f), snapX: true, snapY: true);
        Debug.Assert(snapped == new Vector3(1f, 3f, 5f));
    }

    /// <summary>ponytail: Debug-only prefs double for Save-count asserts.</summary>
    private sealed class RecordingPreferences : IEditorPreferences
    {
        public int SaveCount { get; set; }
        public List<RecentProject> RecentProjects { get; init; } = [];
        public Vector4 BackgroundColor { get; set; }
        public bool ShowColliderBounds { get; set; }
        public bool ShowFPS { get; set; }
        public float HdrExposure { get; set; } = 1.0f;
        public bool SnapEnabled { get; set; }
        public float GridStep { get; set; } = 1.0f;
        public void AddRecentProject(string path, string name) { }
        public void RemoveRecentProject(string path) { }
        public IReadOnlyList<RecentProject> GetRecentProjects() => RecentProjects;
        public void ClearRecentProjects() { }
        public void Save() => SaveCount++;
        public void Dispose() { }
    }
}
