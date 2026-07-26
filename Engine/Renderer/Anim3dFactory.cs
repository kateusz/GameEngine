using Engine.Core;
using Serilog;

namespace Engine.Renderer;

/// <summary>Path-keyed cache for cooked *.anim3d assets. Consumers must not dispose returned assets.</summary>
public interface IAnim3dFactory
{
    Anim3dAsset? Create(string path);

    void Evict(string path);
}

internal sealed class Anim3dFactory : IAnim3dFactory, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<Anim3dFactory>();
    private readonly Dictionary<string, Anim3dAsset> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();
    private bool _disposed;

    public void Evict(string path)
    {
        var normalizedPath = Path.GetFullPath(path);
        lock (_cacheLock)
            _cache.Remove(normalizedPath);
    }

    public Anim3dAsset? Create(string path)
    {
        var normalizedPath = Path.GetFullPath(path);

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(normalizedPath, out var cached))
                return cached;

            if (!PathBuilder.IsUnderAssets(normalizedPath))
            {
                Logger.Warning("Rejected anim3d path outside assets root: {Path}", normalizedPath);
                return null;
            }

            if (!File.Exists(normalizedPath))
            {
                Logger.Warning("Anim3d file not found: {Path}", normalizedPath);
                return null;
            }

            try
            {
                using var stream = File.OpenRead(normalizedPath);
                var asset = Anim3dReader.Read(stream);
                _cache[normalizedPath] = asset;
                return asset;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to load anim3d: {Path}", normalizedPath);
                return null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_cacheLock)
            _cache.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
