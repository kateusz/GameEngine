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

    public bool IsBusy => _inFlight > 0 || _active != null || !_ready.IsEmpty;
    public string? BusyName { get; private set; }

    public void Request(string resolvedPath, Action<Model?> onReady)
    {
        Interlocked.Increment(ref _inFlight);
        var normalized = Path.GetFullPath(resolvedPath);
        BusyName = Path.GetFileName(normalized);
        Task.Run(() =>
        {
            try
            {
                _ready.Enqueue(PrepareOnWorker(normalized, onReady));
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
        }

        if (_active is not { } active)
            return;

        if (active.Textures.Count > 0 && _textureUploadIndex < active.Textures.Count)
        {
            var tex = active.Textures[_textureUploadIndex++];
            textureFactory.CreateFromRgba(tex.Rgba, tex.Width, tex.Height, tex.SRgb, tex.Path);
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

        active.OnReady(model);
        _active = null;
        if (!IsBusy)
            BusyName = null;
    }

    private Prepared PrepareOnWorker(string normalizedPath, Action<Model?> onReady)
    {
        try
        {
            if (!RuntimeMeshBuilder.TryEnsureUpToDate(normalizedPath))
                return Prepared.Fail(onReady);

            var siblingPath = RuntimeMeshPaths.SiblingPath(normalizedPath);
            if (!File.Exists(siblingPath))
                return Prepared.Fail(onReady);

            var bytes = File.ReadAllBytes(siblingPath);
            if (!RuntimeMeshReader.TryRead(bytes, out var source) || source == null)
                return Prepared.Fail(onReady);

            return new Prepared(normalizedPath, bytes, DecodeTextures(source, normalizedPath), onReady);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to prepare model {Path}", normalizedPath);
            return Prepared.Fail(onReady);
        }
    }

    private List<DecodedTexture> DecodeTextures(SourceModel source, string sourcePath)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
        var unique = new Dictionary<(string Path, bool SRgb), DecodedTexture>();

        foreach (var (relative, sRgb) in RuntimeMeshTextureRefs.Enumerate(source))
        {
            var absolute = RuntimeMeshPaths.ResolveFromSourceDirectory(sourceDirectory, relative);
            if (!File.Exists(absolute))
                continue;

            var key = (absolute, sRgb);
            if (unique.ContainsKey(key))
                continue;

            var (data, width, height) = textureFactory.Decode(absolute, sRgb);
            unique[key] = new DecodedTexture(absolute, sRgb, data, width, height);
        }

        return [.. unique.Values];
    }

    private readonly record struct DecodedTexture(string Path, bool SRgb, byte[] Rgba, int Width, int Height);

    private readonly record struct Prepared(
        string Path,
        byte[]? MeshBytes,
        List<DecodedTexture> Textures,
        Action<Model?> OnReady)
    {
        public static Prepared Fail(Action<Model?> onReady) => new("", null, [], onReady);
    }
}
