using System.Numerics;
using Engine.Renderer.Cameras;

namespace Editor.Features.Viewport.Tools;

public interface IViewportTool
{
    /// <summary>
    /// The editor mode this tool handles.
    /// </summary>
    EditorMode Mode { get; }
    
    bool IsActive { get; }
    void OnActivate();
    void OnDeactivate();

    /// <summary>
    /// Handle mouse down event in viewport.
    /// </summary>
    /// <param name="mousePos">Mouse position in screen space</param>
    /// <param name="viewportBounds">Viewport bounds [min, max]</param>
    /// <param name="camera">Orthographic camera</param>
    void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera);

    /// <summary>
    /// Handle mouse move event in viewport.
    /// </summary>
    /// <param name="mousePos">Mouse position in screen space</param>
    /// <param name="viewportBounds">Viewport bounds [min, max]</param>
    /// <param name="camera">Orthographic camera</param>
    void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera);

    /// <summary>
    /// Handle mouse up event in viewport.
    /// </summary>
    /// <param name="mousePos">Mouse position in screen space</param>
    /// <param name="viewportBounds">Viewport bounds [min, max]</param>
    /// <param name="camera">Orthographic camera</param>
    void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera);

    /// <summary>
    /// Render tool-specific overlays (gizmos, measurements, selection indicators, etc.).
    /// </summary>
    /// <param name="viewportBounds">Viewport bounds [min, max]</param>
    /// <param name="camera">Orthographic camera</param>
    void Render(Vector2[] viewportBounds, OrthographicCamera camera);
}
