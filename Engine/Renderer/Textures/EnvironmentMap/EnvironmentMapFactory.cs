using Engine.Platform.OpenGL;
using Engine.Renderer.Meshes;
using Engine.Renderer.Shaders;
using Serilog;

namespace Engine.Renderer.Textures.EnvironmentMap;

// ponytail: no locks — generate and lookup happen on the render thread only
internal sealed class EnvironmentMapFactory(
    IRendererApiConfig apiConfig,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory) : IEnvironmentMapFactory, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<EnvironmentMapFactory>();

    private readonly Dictionary<string, EnvironmentMap?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private OpenGLEnvironmentGenerator? _generator;
    private Texture2D? _brdfLut;
    private TextureCube? _blackCubemap;
    private bool _disposed;

    private OpenGLEnvironmentGenerator Generator => _generator ??= apiConfig.Type switch
    {
        ApiType.SilkNet => new OpenGLEnvironmentGenerator(shaderFactory, meshFactory),
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
                map = Generator.Generate(resolvedHdrPath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to generate environment map from {Path}", resolvedHdrPath);
        }

        _cache[resolvedHdrPath] = map;
        return map;
    }

    public Texture2D GetBrdfLut() => _brdfLut ??= Generator.GenerateBrdfLut();

    public TextureCube GetBlackCubemap() => _blackCubemap ??= OpenGLTextureCube.CreateBlack();

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var map in _cache.Values)
            map?.Dispose();
        _cache.Clear();

        _brdfLut?.Dispose();
        _brdfLut = null;

        _blackCubemap?.Dispose();
        _blackCubemap = null;
        _disposed = true;
    }
}
