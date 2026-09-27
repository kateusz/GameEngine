using System.Collections.Concurrent;
using Engine.Renderer.Textures;

namespace Editor.AssetPicker;

/// <summary>
/// Background DecodePreview → GPU upload, capped per frame. Clear() drops all textures.
/// </summary>
public sealed class AssetThumbnailCache(ITextureFactory textureFactory) : IDisposable
{
    private const int MaxUploadsPerFrame = 8;
    private const int MaxReady = 32;

    private readonly Dictionary<string, Texture2D> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _failed = new(StringComparer.OrdinalIgnoreCase);
    private readonly BlockingCollection<(string Path, int Generation)> _decodeQueue = new();
    private readonly BlockingCollection<(string Path, byte[]? Rgba, int Width, int Height, int Generation)> _ready =
        new(MaxReady);

    private Task? _worker;
    private int _generation;
    private bool _disposed;

    /// <summary>
    /// Returns cached texture, or queues a decode (once). Paths whose decode already
    /// failed are remembered so they are never re-decoded — caller falls back to the
    /// filename button. Returns null while a decode is pending.
    /// </summary>
    public Texture2D? GetOrRequest(string absolutePath)
    {
        if (_cache.TryGetValue(absolutePath, out var cached))
            return cached;

        if (_failed.Contains(absolutePath))
            return null;

        if (_pending.Add(absolutePath))
        {
            EnsureWorker();
            if (!_decodeQueue.IsAddingCompleted)
                _decodeQueue.Add((absolutePath, _generation));
        }

        return null;
    }

    public void Pump()
    {
        var processed = 0;
        while (processed < MaxUploadsPerFrame && _ready.TryTake(out var item))
        {
            if (item.Generation != _generation || _cache.ContainsKey(item.Path))
                continue;

            processed++;
            _pending.Remove(item.Path);

            if (item.Rgba is null)
            {
                _failed.Add(item.Path);
                continue;
            }

            try
            {
                _cache[item.Path] = textureFactory.CreateFromRgba(item.Rgba, item.Width, item.Height);
            }
            catch
            {
                _failed.Add(item.Path);
            }
        }
    }

    public void Clear()
    {
        _generation++;
        while (_decodeQueue.TryTake(out _))
        {
        }

        while (_ready.TryTake(out _))
        {
        }

        _pending.Clear();
        _failed.Clear();

        foreach (var texture in _cache.Values)
            texture.Dispose();
        _cache.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _decodeQueue.CompleteAdding();
        _ready.CompleteAdding();
        _worker?.Wait(TimeSpan.FromSeconds(1));
        Clear();
        _decodeQueue.Dispose();
        _ready.Dispose();
    }

    private void EnsureWorker()
    {
        if (_worker is not null || _decodeQueue.IsAddingCompleted)
            return;

        _worker = Task.Run(DecodeLoop);
    }

    private void DecodeLoop()
    {
        foreach (var (path, generation) in _decodeQueue.GetConsumingEnumerable())
        {
            byte[]? rgba = null;
            var width = 0;
            var height = 0;
            try
            {
                var preview = textureFactory.DecodePreview(path);
                rgba = preview.Data;
                width = preview.Width;
                height = preview.Height;
            }
            catch
            {
            }

            try
            {
                _ready.Add((path, rgba, width, height, generation));
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
