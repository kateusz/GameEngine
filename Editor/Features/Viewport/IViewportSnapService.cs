using System.Numerics;

namespace Editor.Features.Viewport;

/// <summary>
/// Editor snap policy façade: sticky enable/step over prefs, bypass gate, and world lattice quantize.
/// </summary>
public interface IViewportSnapService
{
    /// <summary>Sticky snap enable; mirrors <c>IEditorPreferences.SnapEnabled</c> and persists on set.</summary>
    bool Enabled { get; set; }

    /// <summary>World-space lattice step; mirrors prefs and persists on set.</summary>
    float GridStep { get; set; }

    /// <summary>True when snap is enabled and the temporary bypass modifier is not held.</summary>
    bool ShouldSnap(bool bypassHeld);

    /// <summary>Quantize world XY for masked axes via SnapMath; Z unchanged. Hot path — does not Save.</summary>
    Vector3 SnapWorldPosition(Vector3 worldPosition, bool snapX, bool snapY);
}
