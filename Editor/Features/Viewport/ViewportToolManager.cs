using System.Numerics;
using ECS;
using Editor.Features.Viewport.Tools;
using Engine.Renderer.Cameras;

namespace Editor.Features.Viewport;

/// <summary>
/// Manages viewport interaction tools and delegates mouse events to the active tool.
/// Centralizes tool lifecycle and mode switching.
/// </summary>
public class ViewportToolManager(
    SelectionTool selectionTool,
    MoveTool moveTool,
    ScaleTool scaleTool,
    RulerTool rulerTool)
{
    private readonly Dictionary<EditorMode, IViewportTool> _tools = new()
    {
        { EditorMode.Select, selectionTool },
        { EditorMode.Move, moveTool },
        { EditorMode.Scale, scaleTool },
        { EditorMode.Ruler, rulerTool }
    };

    private EditorMode _currentMode = EditorMode.Select;
    private IViewportTool _activeTool = selectionTool;

    /// <summary>
    /// Switches to a different editor mode and activates the corresponding tool.
    /// </summary>
    public void SetMode(EditorMode mode)
    {
        if (_currentMode == mode)
            return;

        // Don't switch to Tilemap mode - it's handled by TileMapPanel
        if (mode == EditorMode.Tilemap)
            return;
        
        _activeTool.OnDeactivate();
        _currentMode = mode;
        _activeTool = _tools[mode];
        _activeTool.OnActivate();
    }

    /// <summary>
    /// Updates the target entity for manipulation tools (Move, Scale).
    /// Called when entity selection changes.
    /// </summary>
    public void SetTargetEntity(Entity? entity)
    {
        foreach (var tool in _tools.Values.OfType<IEntityTargetTool>())
        {
            tool.SetTargetEntity(entity);
        }
    }

    /// <summary>
    /// Updates the hovered entity for the selection tool.
    /// Called by EditorLayer based on mouse picking.
    /// </summary>
    public void SetHoveredEntity(Entity? entity)
    {
        foreach (var tool in _tools.Values.OfType<IEntityHoverTool>())
        {
            tool.HoveredEntity = entity;
        }
    }

    /// <summary>
    /// Subscribes a handler to be called when an entity is selected in the viewport.
    /// </summary>
    public void SubscribeToEntitySelection(Action<Entity> handler) => selectionTool.OnEntitySelected += handler;

    /// <summary>
    /// Unsubscribes a handler from entity selection notifications.
    /// </summary>
    public void UnsubscribeFromEntitySelection(Action<Entity> handler) => selectionTool.OnEntitySelected -= handler;

    /// <summary>
    /// Handles mouse down event and delegates to active tool.
    /// </summary>
    public void HandleMouseDown(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        _activeTool.OnMouseDown(mousePos, viewportBounds, camera);
    }

    /// <summary>
    /// Handles mouse move event and delegates to active tool.
    /// </summary>
    public void HandleMouseMove(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        _activeTool.OnMouseMove(mousePos, viewportBounds, camera);
    }

    /// <summary>
    /// Handles mouse up event and delegates to active tool.
    /// </summary>
    public void HandleMouseUp(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        _activeTool.OnMouseUp(mousePos, viewportBounds, camera);
    }

    /// <summary>
    /// Renders the active tool's overlays (gizmos, measurements, etc.).
    /// </summary>
    public void RenderActiveTool(Vector2[] viewportBounds, OrthographicCamera camera)
    {
        _activeTool.Render(viewportBounds, camera);
    }

    /// <summary>
    /// Gets a specific tool by type.
    /// </summary>
    public T? GetTool<T>() where T : class, IViewportTool
    {
        return _tools.Values.OfType<T>().FirstOrDefault();
    }
}
