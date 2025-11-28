namespace Engine.Core.Window;

/// <summary>
/// Factory interface for creating game window instances.
/// </summary>
public interface IGameWindowFactory
{
    /// <summary>
    /// Creates a game window from a window instance.
    /// </summary>
    /// <param name="window">The underlying window implementation.</param>
    /// <returns>A game window instance.</returns>
    IGameWindow Create();
}
