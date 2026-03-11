using System.Numerics;
using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Renderer;

/// <summary>
/// Precomputes IBL (Image-Based Lighting) maps from an HDR environment map.
/// Produces irradiance cubemap, prefiltered environment cubemap, and BRDF LUT.
/// </summary>
internal sealed class IBLPrecomputer : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<IBLPrecomputer>();

    private const uint IrradianceSize = 32;
    private const uint PrefilterSize = 128;
    private const uint BrdfLutSize = 512;
    private const int MaxMipLevels = 5;

    private OpenGLCubemap? _irradianceMap;
    private OpenGLCubemap? _prefilterMap;
    private uint _brdfLutTexture;
    private bool _disposed;
    private bool _computed;

    public uint IrradianceMapId => _irradianceMap?.TextureId ?? 0;
    public uint PrefilterMapId => _prefilterMap?.TextureId ?? 0;
    public uint BrdfLutId => _brdfLutTexture;
    public bool IsReady => _computed;

    // Cube vertices for rendering into cubemap faces
    private static readonly float[] CubeVertices =
    [
        -1.0f,  1.0f, -1.0f,  -1.0f, -1.0f, -1.0f,   1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,   1.0f,  1.0f, -1.0f,  -1.0f,  1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,  -1.0f, -1.0f, -1.0f,  -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,  -1.0f,  1.0f,  1.0f,  -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f, -1.0f,   1.0f, -1.0f,  1.0f,   1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,   1.0f,  1.0f, -1.0f,   1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,  -1.0f,  1.0f,  1.0f,   1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,   1.0f, -1.0f,  1.0f,  -1.0f, -1.0f,  1.0f,
        -1.0f,  1.0f, -1.0f,   1.0f,  1.0f, -1.0f,   1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,  -1.0f,  1.0f,  1.0f,  -1.0f,  1.0f, -1.0f,
        -1.0f, -1.0f, -1.0f,  -1.0f, -1.0f,  1.0f,   1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,  -1.0f, -1.0f,  1.0f,   1.0f, -1.0f,  1.0f
    ];

    // Quad vertices for BRDF LUT (position xyz + texcoord uv)
    private static readonly float[] QuadVertices =
    [
        -1.0f,  1.0f, 0.0f,  0.0f, 1.0f,
        -1.0f, -1.0f, 0.0f,  0.0f, 0.0f,
         1.0f,  1.0f, 0.0f,  1.0f, 1.0f,
         1.0f, -1.0f, 0.0f,  1.0f, 0.0f
    ];

    // 6 view matrices for cubemap face capture (column-major for row-vector convention)
    private static readonly Matrix4x4[] CaptureViews =
    [
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3( 1.0f,  0.0f,  0.0f), new Vector3(0.0f, -1.0f,  0.0f)),
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(-1.0f,  0.0f,  0.0f), new Vector3(0.0f, -1.0f,  0.0f)),
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3( 0.0f,  1.0f,  0.0f), new Vector3(0.0f,  0.0f,  1.0f)),
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3( 0.0f, -1.0f,  0.0f), new Vector3(0.0f,  0.0f, -1.0f)),
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3( 0.0f,  0.0f,  1.0f), new Vector3(0.0f, -1.0f,  0.0f)),
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3( 0.0f,  0.0f, -1.0f), new Vector3(0.0f, -1.0f,  0.0f))
    ];

    private static readonly Matrix4x4 CaptureProjection =
        Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2.0f, 1.0f, 0.1f, 10.0f);

    public void Compute(string hdrPath, IShaderFactory shaderFactory)
    {
        if (_computed)
        {
            Logger.Warning("IBL maps already computed, disposing old ones");
            DisposeResources();
        }

        Logger.Information("Computing IBL maps from {Path}...", hdrPath);

        var gl = SilkNetContext.GL;

        // Save current GL state
        var previousFbo = (uint)gl.GetInteger(GLEnum.DrawFramebufferBinding);
        var previousViewport = new int[4];
        gl.GetInteger(GLEnum.Viewport, previousViewport);

        // Create capture FBO and RBO
        var captureFbo = gl.GenFramebuffer();
        var captureRbo = gl.GenRenderbuffer();

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);

        // Create cube VAO/VBO
        var cubeVao = gl.GenVertexArray();
        var cubeVbo = gl.GenBuffer();
        SetupCubeBuffers(gl, cubeVao, cubeVbo);

        // Load HDR texture
        using var hdrTexture = OpenGLTexture2D.CreateHDR(hdrPath);

        // Step 1: Convert equirectangular HDR to cubemap
        var envCubemap = ConvertEquirectToCubemap(gl, hdrTexture, shaderFactory, captureFbo, captureRbo, cubeVao);

        // Step 2: Generate irradiance map
        _irradianceMap = ComputeIrradianceMap(gl, envCubemap, shaderFactory, captureFbo, captureRbo, cubeVao);

        // Step 3: Generate prefiltered environment map
        _prefilterMap = ComputePrefilterMap(gl, envCubemap, shaderFactory, captureFbo, captureRbo, cubeVao);

        // Step 4: Generate BRDF LUT
        _brdfLutTexture = ComputeBrdfLut(gl, shaderFactory, captureFbo, captureRbo);

        // Clean up the environment cubemap (no longer needed)
        envCubemap.Dispose();

        // Clean up capture resources
        gl.DeleteBuffer(cubeVbo);
        gl.DeleteVertexArray(cubeVao);
        gl.DeleteRenderbuffer(captureRbo);
        gl.DeleteFramebuffer(captureFbo);

        // Restore GL state
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, previousFbo);
        gl.Viewport(previousViewport[0], previousViewport[1],
            (uint)previousViewport[2], (uint)previousViewport[3]);

        _computed = true;
        Logger.Information("IBL maps computed successfully (irradiance={Irr}x{Irr}, prefilter={Pre}x{Pre}, brdfLut={Brdf}x{Brdf})",
            IrradianceSize, IrradianceSize, PrefilterSize, PrefilterSize, BrdfLutSize, BrdfLutSize);
    }

    private static unsafe void SetupCubeBuffers(GL gl, uint vao, uint vbo)
    {
        gl.BindVertexArray(vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        fixed (float* ptr = CubeVertices)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(CubeVertices.Length * sizeof(float)),
                ptr, BufferUsageARB.StaticDraw);
        }

        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        gl.BindVertexArray(0);
    }

    private static OpenGLCubemap ConvertEquirectToCubemap(GL gl, Texture2D hdrTexture,
        IShaderFactory shaderFactory, uint fbo, uint rbo, uint cubeVao)
    {
        const uint envSize = 512;
        var envCubemap = new OpenGLCubemap(envSize, InternalFormat.Rgb16f);

        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, envSize, envSize);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, rbo);

        var shader = shaderFactory.Create(
            "assets/shaders/opengl/equirect_to_cubemap.vert",
            "assets/shaders/opengl/equirect_to_cubemap.frag");

        shader.Bind();
        shader.SetInt("u_EquirectangularMap", 0);
        shader.SetMat4("u_Projection", CaptureProjection);

        hdrTexture.Bind(0);

        gl.Viewport(0, 0, envSize, envSize);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        for (var i = 0; i < 6; i++)
        {
            shader.SetMat4("u_View", CaptureViews[i]);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, envCubemap.TextureId, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            gl.BindVertexArray(cubeVao);
            gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }

        shader.Unbind();
        return envCubemap;
    }

    private static OpenGLCubemap ComputeIrradianceMap(GL gl, OpenGLCubemap envCubemap,
        IShaderFactory shaderFactory, uint fbo, uint rbo, uint cubeVao)
    {
        var irradianceMap = new OpenGLCubemap(IrradianceSize, InternalFormat.Rgb16f);

        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24,
            IrradianceSize, IrradianceSize);

        var shader = shaderFactory.Create(
            "assets/shaders/opengl/irradiance_convolution.vert",
            "assets/shaders/opengl/irradiance_convolution.frag");

        shader.Bind();
        shader.SetInt("u_EnvironmentMap", 0);
        shader.SetMat4("u_Projection", CaptureProjection);

        envCubemap.Bind(0);

        gl.Viewport(0, 0, IrradianceSize, IrradianceSize);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        for (var i = 0; i < 6; i++)
        {
            shader.SetMat4("u_View", CaptureViews[i]);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, irradianceMap.TextureId, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            gl.BindVertexArray(cubeVao);
            gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }

        shader.Unbind();
        return irradianceMap;
    }

    private static OpenGLCubemap ComputePrefilterMap(GL gl, OpenGLCubemap envCubemap,
        IShaderFactory shaderFactory, uint fbo, uint rbo, uint cubeVao)
    {
        var prefilterMap = new OpenGLCubemap(PrefilterSize, InternalFormat.Rgb16f, generateMipmaps: true);

        var shader = shaderFactory.Create(
            "assets/shaders/opengl/prefilter.vert",
            "assets/shaders/opengl/prefilter.frag");

        shader.Bind();
        shader.SetInt("u_EnvironmentMap", 0);
        shader.SetMat4("u_Projection", CaptureProjection);

        envCubemap.Bind(0);

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        for (var mip = 0; mip < MaxMipLevels; mip++)
        {
            var mipWidth = (uint)(PrefilterSize * MathF.Pow(0.5f, mip));
            var mipHeight = mipWidth;
            if (mipWidth < 1) mipWidth = 1;
            if (mipHeight < 1) mipHeight = 1;

            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24,
                mipWidth, mipHeight);

            gl.Viewport(0, 0, mipWidth, mipHeight);

            var roughness = (float)mip / (MaxMipLevels - 1);
            shader.SetFloat("u_Roughness", roughness);

            for (var i = 0; i < 6; i++)
            {
                shader.SetMat4("u_View", CaptureViews[i]);
                gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    TextureTarget.TextureCubeMapPositiveX + i, prefilterMap.TextureId, mip);
                gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                gl.BindVertexArray(cubeVao);
                gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
            }
        }

        shader.Unbind();
        return prefilterMap;
    }

    private unsafe uint ComputeBrdfLut(GL gl, IShaderFactory shaderFactory, uint fbo, uint rbo)
    {
        var brdfLutTexture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, brdfLutTexture);
        // GL_RG16F = 0x822F, GL_RG = 0x8227
        gl.TexImage2D(TextureTarget.Texture2D, 0, (InternalFormat)0x822F,
            BrdfLutSize, BrdfLutSize, 0, (PixelFormat)0x8227, PixelType.Float, null);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        // Setup quad VAO
        var quadVao = gl.GenVertexArray();
        var quadVbo = gl.GenBuffer();
        gl.BindVertexArray(quadVao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, quadVbo);

        fixed (float* ptr = QuadVertices)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(QuadVertices.Length * sizeof(float)),
                ptr, BufferUsageARB.StaticDraw);
        }

        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24,
            BrdfLutSize, BrdfLutSize);
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, brdfLutTexture, 0);
        gl.DrawBuffer(DrawBufferMode.ColorAttachment0);

        gl.Viewport(0, 0, BrdfLutSize, BrdfLutSize);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var shader = shaderFactory.Create(
            "assets/shaders/opengl/brdf_lut.vert",
            "assets/shaders/opengl/brdf_lut.frag");

        shader.Bind();
        gl.BindVertexArray(quadVao);
        gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        shader.Unbind();

        gl.DeleteBuffer(quadVbo);
        gl.DeleteVertexArray(quadVao);

        return brdfLutTexture;
    }

    public void BindIrradiance(int slot) => _irradianceMap?.Bind(slot);
    public void BindPrefilter(int slot) => _prefilterMap?.Bind(slot);

    public void BindBrdfLut(int slot)
    {
        if (_brdfLutTexture == 0) return;
        var gl = SilkNetContext.GL;
        gl.ActiveTexture(TextureUnit.Texture0 + slot);
        gl.BindTexture(TextureTarget.Texture2D, _brdfLutTexture);
    }

    private void DisposeResources()
    {
        _irradianceMap?.Dispose();
        _irradianceMap = null;

        _prefilterMap?.Dispose();
        _prefilterMap = null;

        if (_brdfLutTexture != 0)
        {
            SilkNetContext.GL.DeleteTexture(_brdfLutTexture);
            _brdfLutTexture = 0;
        }

        _computed = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        DisposeResources();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
