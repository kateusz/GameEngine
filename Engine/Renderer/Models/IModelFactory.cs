namespace Engine.Renderer.Models;

/// <summary>
/// Creates GPU-ready models from imported <c>.mesh</c> paths only.
/// Returns null on missing/corrupt/rejected paths (pipeline owns fail-soft cube).
/// </summary>
public interface IModelFactory
{
    /// <summary>Loads <paramref name="path"/> when extension is <c>.mesh</c>; otherwise rejects.</summary>
    Model? Create(string path);
}
