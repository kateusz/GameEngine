using Engine.Core;
using Engine.Renderer.Skeletal.Serialization;
using Serilog;

namespace Engine.Renderer.Skeletal;

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
        }

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

        Anim3dAsset asset;
        try
        {
            using var stream = File.OpenRead(normalizedPath);
            asset = Anim3dReader.Read(stream);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load anim3d: {Path}", normalizedPath);
            return null;
        }

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(normalizedPath, out var raced))
                return raced;
            _cache[normalizedPath] = asset;
            return asset;
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
