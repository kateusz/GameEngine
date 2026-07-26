namespace Engine.Renderer;

/// <summary>
/// Creates GPU-ready models from cooked <c>.mesh</c> paths only.
/// Returns null on missing/corrupt/rejected paths (pipeline owns fail-soft cube).
/// </summary>
public interface IModelFactory
{
    /// <summary>Loads <paramref name="path"/> when extension is <c>.mesh</c>; otherwise rejects.</summary>
    Model? Create(string path);

    /// <summary>Drops a cached model so the next <see cref="Create"/> reloads from disk.</summary>
    void Evict(string path);

    /// <summary>Clears all cached GPU models (e.g. after vertex layout change).</summary>
    void ClearCache();
}
