using System.Collections.Concurrent;
using Engine.Renderer.Models;
using Engine.Renderer.Models.RuntimeMesh;
using Engine.Renderer.Textures;
using Serilog;

namespace Editor.Features.Models;

public sealed class EditorModelLoadService(IModelFactory modelFactory, ITextureFactory textureFactory)
{
    private static readonly ILogger Logger = Log.ForContext<EditorModelLoadService>();
    private readonly ConcurrentQueue<Prepared> _ready = new();
    private int _inFlight;
    private Prepared? _active;
    private int _textureUploadIndex;
    private int _advancedForActive;

    public bool IsBusy => _inFlight > 0 || _active != null || !_ready.IsEmpty;
    public string? BusyName { get; private set; }

    public void Request(string resolvedPath, Action<Model?> onReady, EditorLoadWorkMeter? meter = null)
    {
        Interlocked.Increment(ref _inFlight);
        var normalized = Path.GetFullPath(resolvedPath);
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
            _textureUploadIndex = 0;
            _advancedForActive = prepared.AdvancedOnPrepare;
        }

        if (_active is not { } active)
            return;

        if (active.Textures.Count > 0 && _textureUploadIndex < active.Textures.Count)
        {
            var tex = active.Textures[_textureUploadIndex++];
            textureFactory.CreateFromRgba(tex.Rgba, tex.Width, tex.Height, tex.SRgb, tex.Path);
            active.Meter?.Advance();
            _advancedForActive++;
            BusyName = Path.GetFileName(tex.Path);
            if (_textureUploadIndex < active.Textures.Count)
                return;
        }

        Model? model = null;
        try
        {
            if (active.MeshBytes != null)
                model = modelFactory.Create(active.Path, active.MeshBytes);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load model {Path}", active.Path);
        }

        active.Meter?.FillJob(active.ExpectedUnits, _advancedForActive);

        try
        {
            active.OnReady(model);
        }
        finally
        {
            _active = null;
            _advancedForActive = 0;
            if (!IsBusy)
                BusyName = null;
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

            var textures = DecodeTextures(source, normalizedPath);
            var submeshes = source.Submeshes.Count;
            var units = submeshes + textures.Count;
            if (units == 0)
                units = 1;

            meter?.Expect(units);
            var advanced = 0;
            if (submeshes > 0)
            {
                meter?.Advance(submeshes);
                advanced = submeshes;
            }
            else if (textures.Count == 0)
            {
                meter?.Advance();
                advanced = 1;
            }

            return new Prepared(normalizedPath, bytes, textures, onReady, meter, units, advanced);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to prepare model {Path}", normalizedPath);
            return FailPrepared(onReady, meter);
        }
    }

    private static Prepared FailPrepared(Action<Model?> onReady, EditorLoadWorkMeter? meter)
    {
        meter?.Expect(1);
        meter?.Advance();
        return new Prepared("", null, [], onReady, null, 0, 0);
    }

    private List<DecodedTexture> DecodeTextures(SourceModel source, string sourcePath)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
        var jobs = new List<(string Absolute, bool SRgb)>();
        var seen = new HashSet<(string Path, bool SRgb)>();

        foreach (var (relative, sRgb) in RuntimeMeshTextureRefs.Enumerate(source))
        {
            var absolute = RuntimeMeshPaths.ResolveFromSourceDirectory(sourceDirectory, relative);
            if (!File.Exists(absolute))
                continue;

            var key = (absolute, sRgb);
            if (!seen.Add(key))
                continue;

            jobs.Add((absolute, sRgb));
        }

        if (jobs.Count == 0)
            return [];

        var decoded = new DecodedTexture[jobs.Count];
        Parallel.For(0, jobs.Count, i =>
        {
            var (absolute, sRgb) = jobs[i];
            try
            {
                var (data, width, height) = textureFactory.Decode(absolute, sRgb);
                decoded[i] = new DecodedTexture(absolute, sRgb, data, width, height);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to decode texture {Path}", absolute);
                decoded[i] = default;
            }
        });

        return decoded.Where(t => t.Rgba != null).ToList();
    }

    private readonly record struct DecodedTexture(string Path, bool SRgb, byte[] Rgba, int Width, int Height);

    private readonly record struct Prepared(
        string Path,
        byte[]? MeshBytes,
        List<DecodedTexture> Textures,
        Action<Model?> OnReady,
        EditorLoadWorkMeter? Meter,
        int ExpectedUnits,
        int AdvancedOnPrepare);
}
