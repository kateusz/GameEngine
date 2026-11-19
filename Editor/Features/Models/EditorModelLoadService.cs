using System.Collections.Concurrent;
using Engine.Renderer.Models;
using Engine.Renderer.Models.RuntimeMesh;
using Engine.Renderer.Textures;
using Engine.Renderer.Textures.Compression;
using Serilog;

namespace Editor.Features.Models;

public sealed class EditorModelLoadService(IModelFactory modelFactory, ITextureFactory textureFactory)
{
    private static readonly ILogger Logger = Log.ForContext<EditorModelLoadService>();
    private readonly ConcurrentQueue<Prepared> _ready = new();
    private int _inFlight;
    private Prepared? _active;
    private int _textureIndex;
    private int _advanceCount;

    public bool IsBusy => _inFlight > 0 || _active != null || !_ready.IsEmpty;
    public string? BusyName { get; private set; }

    public void Request(string resolvedPath, Action<Model?> onReady, EditorLoadWorkMeter? meter = null)
    {
        var normalized = Path.GetFullPath(resolvedPath);

        // Same-session re-open: model already GPU-ready — skip Assimp/stamp/read entirely.
        if (modelFactory.IsCached(normalized))
        {
            meter?.Advance();
            onReady(SafeCreate(normalized, null));
            return;
        }

        Interlocked.Increment(ref _inFlight);
        BusyName = Path.GetFileName(normalized);
        Task.Run(() =>
        {
            try
            {
                _ready.Enqueue(PrepareOnWorker(normalized, onReady, meter));
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        });
    }

    public void Pump()
    {
        if (_active == null && _ready.TryDequeue(out var prepared))
        {
            _active = prepared;
            _textureIndex = 0;
            _advanceCount = prepared.PreparedUnits;
        }

        if (_active is not { } active)
            return;

        // One texture per frame. Cooked ones upload their BC sidecar; the rest arrive decoded
        // from the worker so no raster decode runs on the render thread.
        if (_textureIndex < active.Textures.Count)
        {
            var texture = active.Textures[_textureIndex++];
            try
            {
                if (texture.Rgba != null)
                    textureFactory.CreateFromRgba(texture.Rgba, texture.Width, texture.Height, texture.SRgb,
                        texture.Path);
                else
                    textureFactory.Create(texture.Path, texture.SRgb);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to load texture {Path}", texture.Path);
            }

            active.Meter?.Advance();
            _advanceCount++;
            BusyName = Path.GetFileName(texture.Path);
            if (_textureIndex < active.Textures.Count)
                return;
        }

        var model = active.Path.Length == 0 ? null : SafeCreate(active.Path, active.MeshBytes);
        active.Meter?.FillJob(active.Units, _advanceCount);

        try
        {
            active.OnReady(model);
        }
        finally
        {
            _active = null;
            if (!IsBusy)
                BusyName = null;
        }
    }

    private Model? SafeCreate(string path, byte[]? meshBytes)
    {
        try
        {
            return modelFactory.Create(path, meshBytes);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load model {Path}", path);
            return null;
        }
    }

    private Prepared PrepareOnWorker(string normalizedPath, Action<Model?> onReady, EditorLoadWorkMeter? meter)
    {
        try
        {
            if (!RuntimeMeshBuilder.TryEnsureUpToDate(normalizedPath))
                return FailPrepared(onReady, meter);

            var siblingPath = RuntimeMeshPaths.SiblingPath(normalizedPath);
            if (!File.Exists(siblingPath))
                return FailPrepared(onReady, meter);

            var bytes = File.ReadAllBytes(siblingPath);
            if (!RuntimeMeshReader.TryRead(bytes, out var source) || source == null)
                return FailPrepared(onReady, meter);

            var textures = CollectTextures(source, normalizedPath);
            var submeshes = source.Submeshes.Count;
            var units = System.Math.Max(submeshes + textures.Count, 1);

            // Scene already Expect(1) provisional per model — add the rest.
            if (units > 1)
                meter?.Expect(units - 1);

            var prepared = 0;
            if (submeshes > 0)
            {
                meter?.Advance(submeshes);
                prepared = submeshes;
            }

            return new Prepared(normalizedPath, bytes, textures, onReady, meter, units, prepared);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to prepare model {Path}", normalizedPath);
            return FailPrepared(onReady, meter);
        }
    }

    private List<PendingTexture> CollectTextures(SourceModel source, string sourcePath)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
        var seen = new HashSet<(string Path, bool SRgb)>();
        var pending = new List<PendingTexture>();
        var uncooked = new List<(string Path, bool SRgb)>();

        foreach (var (relative, sRgb) in RuntimeMeshTextureRefs.Enumerate(source))
        {
            var absolute = RuntimeMeshPaths.ResolveFromSourceDirectory(sourceDirectory, relative);
            if (!File.Exists(absolute) || !seen.Add((absolute, sRgb)))
                continue;

            if (HasCookedSidecar(absolute, sRgb))
                pending.Add(new PendingTexture(absolute, sRgb, null, 0, 0));
            else
                uncooked.Add((absolute, sRgb));
        }

        if (uncooked.Count == 0)
            return pending;

        var decoded = new PendingTexture[uncooked.Count];
        Parallel.For(0, uncooked.Count, i =>
        {
            var (path, sRgb) = uncooked[i];
            try
            {
                var (rgba, width, height) = textureFactory.Decode(path, sRgb);
                decoded[i] = new PendingTexture(path, sRgb, rgba, width, height);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to decode texture {Path}", path);
                decoded[i] = new PendingTexture(path, sRgb, null, 0, 0);
            }
        });

        pending.AddRange(decoded);
        return pending;
    }

    // Same freshness rule as TextureBcSidecar.TryReadUsableSidecar, without reading the DDS bytes:
    // a corrupt-but-fresh sidecar just falls back to a decode inside Create (rare).
    private static bool HasCookedSidecar(string path, bool sRgb)
    {
        var sidecar = BcDdsSidecarPath.ForSource(path, sRgb);
        return File.Exists(sidecar)
               && File.GetLastWriteTimeUtc(sidecar) >= File.GetLastWriteTimeUtc(path);
    }

    private static Prepared FailPrepared(Action<Model?> onReady, EditorLoadWorkMeter? meter)
    {
        if (meter != null)
        {
            if (meter.Total == 0)
                meter.Expect(1);
            meter.Advance();
        }

        return new Prepared("", null, [], onReady, null, 0, 0);
    }

    private readonly record struct PendingTexture(
        string Path,
        bool SRgb,
        byte[]? Rgba,
        int Width,
        int Height);

    private readonly record struct Prepared(
        string Path,
        byte[]? MeshBytes,
        List<PendingTexture> Textures,
        Action<Model?> OnReady,
        EditorLoadWorkMeter? Meter,
        int Units,
        int PreparedUnits);
}
