namespace Editor;

/// <summary>
/// Defines the different manipulation modes available in the editor viewport.
/// </summary>
public enum EditorMode
{
    /// <summary>
    /// Selection mode - click to select entities
    /// </summary>
    Select,

    /// <summary>
    /// Move mode - drag entities to reposition them
    /// </summary>
    Move,

    /// <summary>
    /// Scale mode - drag to scale entities
    /// </summary>
    Scale,

    /// <summary>
    /// Ruler mode - measure distances between points in the viewport
    /// </summary>
    Ruler,

    /// <summary>
    /// Tilemap mode - paint tiles on a tilemap
    /// </summary>
    Tilemap
}