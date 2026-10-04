using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Platform.SilkNet;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLSkyCapture : ISkyCapture
{
    private static readonly ILogger Logger = Log.ForContext<OpenGLSkyCapture>();
    private static readonly HashSet<string> Warned = new(StringComparer.OrdinalIgnoreCase);

    private readonly uint _cubemap;
    private readonly uint _equirect;
    private readonly uint _framebuffer;
    private readonly uint _depth;
    private readonly int _size;
    private readonly int[] _previousViewport = new int[4];
    private int _previousFbo;
    private bool _saved;
    private bool _disposed;

    private OpenGLSkyCapture(uint cubemap, uint equirect, uint framebuffer, uint depth, int size)
    {
        _cubemap = cubemap;
        _equirect = equirect;
        _framebuffer = framebuffer;
        _depth = depth;
        _size = size;
    }

    public static bool TryCreate(string absolutePath, out uint cubemapId, out ISkyCapture capture)
    {
        cubemapId = 0;
        capture = null!;

        TextureFileDecoder.DecodedImage decoded;
        try
        {
            decoded = TextureFileDecoder.Decode(absolutePath, sRgb: false);
        }
        catch (Exception ex)
        {
            WarnOnce(absolutePath + "|fail", "Skybox image failed to load ({Path}): {Message}", absolutePath, ex.Message);
            return false;
        }

        if (decoded.Width != decoded.Height * 2)
            WarnOnce(absolutePath + "|aspect", "Skybox image is not 2:1 equirectangular: {Path}", absolutePath);

        var gl = SilkNetContext.GL;
        var previousFbo = gl.GetInteger(GLEnum.FramebufferBinding);
        uint cubemap = 0, equirect = 0, framebuffer = 0, depth = 0;
        try
        {
            cubemap = CreateCubemap(gl, LightingMath.SkyCaptureFaceSize);
            equirect = CreateEquirect(gl, decoded);
            framebuffer = gl.GenFramebuffer();
            depth = gl.GenRenderbuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depth);
            gl.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer,
                InternalFormat.DepthComponent24,
                (uint)LightingMath.SkyCaptureFaceSize,
                (uint)LightingMath.SkyCaptureFaceSize);
            gl.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer,
                depth);
            OpenGLDebug.CheckError(gl, "SkyCapture create");
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)previousFbo);
            UnbindUnit0(gl);

            cubemapId = cubemap;
            capture = new OpenGLSkyCapture(cubemap, equirect, framebuffer, depth, LightingMath.SkyCaptureFaceSize);
            return true;
        }
        catch (Exception ex)
        {
            WarnOnce(absolutePath + "|fail", "Skybox cubemap failed ({Path}): {Message}", absolutePath, ex.Message);
            DeleteAll(gl, cubemap, equirect, framebuffer, depth);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)previousFbo);
            UnbindUnit0(gl);
            cubemapId = 0;
            capture = null!;
            return false;
        }
    }

    public bool BeginFace(int face)
    {
        if (_disposed || (uint)face > 5)
            return false;

        var gl = SilkNetContext.GL;
        if (!_saved)
        {
            _previousFbo = gl.GetInteger(GLEnum.FramebufferBinding);
            gl.GetInteger(GLEnum.Viewport, _previousViewport);
            _saved = true;
        }

        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.TextureCubeMap, 0);
        gl.BindTexture(TextureTarget.Texture2D, _equirect);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        gl.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            TextureTarget.TextureCubeMapPositiveX + face,
            _cubemap,
            0);
        OpenGLDebug.CheckError(gl, "SkyCapture BeginFace");

        if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            return false;

        gl.Viewport(0, 0, (uint)_size, (uint)_size);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var gl = SilkNetContext.GL;
        if (_saved)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)_previousFbo);
            gl.Viewport(
                _previousViewport[0],
                _previousViewport[1],
                (uint)_previousViewport[2],
                (uint)_previousViewport[3]);
        }

        if (_equirect != 0)
            gl.DeleteTexture(_equirect);
        if (_depth != 0)
            gl.DeleteRenderbuffer(_depth);
        if (_framebuffer != 0)
            gl.DeleteFramebuffer(_framebuffer);
        UnbindUnit0(gl);
        OpenGLDebug.CheckError(gl, "SkyCapture Dispose");
    }

    private static uint CreateCubemap(GL gl, int size)
    {
        var id = gl.GenTexture();
        gl.BindTexture(TextureTarget.TextureCubeMap, id);
        for (var face = 0; face < 6; face++)
        {
            unsafe
            {
                gl.TexImage2D(
                    TextureTarget.TextureCubeMapPositiveX + face,
                    0,
                    InternalFormat.Rgba8,
                    (uint)size,
                    (uint)size,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    (void*)0);
            }
        }

        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
        return id;
    }

    private static uint CreateEquirect(GL gl, TextureFileDecoder.DecodedImage decoded)
    {
        var id = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, id);
        unsafe
        {
            fixed (byte* ptr = decoded.Data)
            {
                gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    decoded.InternalFormat,
                    (uint)decoded.Width,
                    (uint)decoded.Height,
                    0,
                    decoded.DataFormat,
                    PixelType.UnsignedByte,
                    ptr);
            }
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        return id;
    }

    private static void UnbindUnit0(GL gl)
    {
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, 0);
        gl.BindTexture(TextureTarget.TextureCubeMap, 0);
    }

    private static void DeleteAll(GL gl, uint cubemap, uint equirect, uint framebuffer, uint depth)
    {
        if (equirect != 0)
            gl.DeleteTexture(equirect);
        if (cubemap != 0)
            gl.DeleteTexture(cubemap);
        if (depth != 0)
            gl.DeleteRenderbuffer(depth);
        if (framebuffer != 0)
            gl.DeleteFramebuffer(framebuffer);
    }

    private static void WarnOnce(string key, string template, string path, string? detail = null)
    {
        lock (Warned)
        {
            if (!Warned.Add(key))
                return;
        }

        if (detail is null)
            Logger.Warning(template, path);
        else
            Logger.Warning(template, path, detail);
    }
}
