namespace Engine.Core.Window;

/// <summary>
/// Factory interface for creating game window instances.
/// </summary>
internal interface IGameWindowFactory
{
    /// <summary>
    /// Creates a game window from a window instance.
    /// </summary>
    /// <returns>A game window instance.</returns>
    IGameWindow Create();
}
