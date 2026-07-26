using Engine.Core;
using Serilog;

namespace Engine.Renderer;

/// <summary>Path-keyed cache for cooked *.skel assets. Consumers must not dispose returned assets.</summary>
public interface ISkeletonFactory
{
    SkeletonAsset? Create(string path);

    void Evict(string path);
}

internal sealed class SkeletonFactory : ISkeletonFactory, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<SkeletonFactory>();
    private readonly Dictionary<string, SkeletonAsset> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();
    private bool _disposed;

    public void Evict(string path)
    {
        var normalizedPath = Path.GetFullPath(path);
        lock (_cacheLock)
            _cache.Remove(normalizedPath);
    }

    public SkeletonAsset? Create(string path)
    {
        var normalizedPath = Path.GetFullPath(path);

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(normalizedPath, out var cached))
                return cached;

            if (!PathBuilder.IsUnderAssets(normalizedPath))
            {
                Logger.Warning("Rejected skeleton path outside assets root: {Path}", normalizedPath);
                return null;
            }

            if (!File.Exists(normalizedPath))
            {
                Logger.Warning("Skeleton file not found: {Path}", normalizedPath);
                return null;
            }

            try
            {
                using var stream = File.OpenRead(normalizedPath);
                var asset = SkeletonReader.Read(stream);
                _cache[normalizedPath] = asset;
                return asset;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to load skeleton: {Path}", normalizedPath);
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
