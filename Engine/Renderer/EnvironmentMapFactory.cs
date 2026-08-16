using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Renderer;

// ponytail: no locks — bake and lookup happen on the render thread only
internal sealed class EnvironmentMapFactory(
    IRendererApiConfig apiConfig,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory) : IEnvironmentMapFactory, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<EnvironmentMapFactory>();

    private readonly Dictionary<string, EnvironmentMap?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private OpenGLEnvironmentBaker? _baker;
    private uint _brdfLut;
    private TextureCube? _blackCubemap;
    private bool _disposed;

    private OpenGLEnvironmentBaker Baker => _baker ??= apiConfig.Type switch
    {
        ApiType.SilkNet => new OpenGLEnvironmentBaker(shaderFactory, meshFactory),
        _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
    };

    public EnvironmentMap? GetOrCreate(string resolvedHdrPath)
    {
        if (_cache.TryGetValue(resolvedHdrPath, out var cached))
            return cached;

        EnvironmentMap? map = null;
        try
        {
            if (!File.Exists(resolvedHdrPath))
                Logger.Error("Environment HDR not found: {Path}", resolvedHdrPath);
            else
                map = Baker.Bake(resolvedHdrPath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to bake environment map from {Path}", resolvedHdrPath);
        }

        _cache[resolvedHdrPath] = map;
        return map;
    }

    public uint GetBrdfLutId() => _brdfLut != 0 ? _brdfLut : _brdfLut = Baker.BakeBrdfLut();

    public TextureCube GetBlackCubemap() => _blackCubemap ??= OpenGLTextureCube.CreateBlack();

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var map in _cache.Values)
            map?.Dispose();
        _cache.Clear();

        if (_brdfLut != 0)
        {
            SilkNetContext.GL.DeleteTexture(_brdfLut);
            _brdfLut = 0;
        }

        _blackCubemap?.Dispose();
        _blackCubemap = null;
        _disposed = true;
    }
}
