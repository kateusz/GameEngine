using Engine.Scene.Components;

namespace Editor.Panels;

/// <summary>
/// Interface for the TileMap editor panel.
/// Provides visual tilemap editing tools with Godot-like functionality.
/// </summary>
public interface ITileMapPanel
{
    /// <summary>
    /// Gets or sets whether the panel is currently open.
    /// </summary>
    bool IsOpen { get; set; }

    /// <summary>
    /// Sets the active tilemap component to edit.
    /// </summary>
    /// <param name="tileMap">The tilemap component to edit, or null to close the editor.</param>
    void SetTileMap(TileMapComponent? tileMap);

    /// <summary>
    /// Checks if the panel is currently editing the specified tilemap component.
    /// </summary>
    /// <param name="tileMap">The tilemap component to check.</param>
    /// <returns>True if the panel is actively editing this tilemap.</returns>
    bool IsActiveFor(TileMapComponent tileMap);

    /// <summary>
    /// Reloads the tileset with current tilemap settings.
    /// </summary>
    void ReloadTileSet();

    /// <summary>
    /// Renders the panel using ImGui.
    /// </summary>
    /// <param name="viewportDockId">Optional dock ID for initial docking to viewport.</param>
    void OnImGuiRender(uint viewportDockId = 0);
}
