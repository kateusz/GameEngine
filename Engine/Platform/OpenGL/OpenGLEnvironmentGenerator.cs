using System.Numerics;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

/// <summary>
/// One-time GPU generation for IBL: equirect .hdr → environment cubemap → irradiance/prefilter maps + BRDF LUT.
/// Uses a private FBO and restores framebuffer/viewport/depth state, so it is safe mid-frame.
/// Sizes and the prefilter mip count come from EnvironmentMapConstants; the IBL shaders receive
/// them as injected #defines (see OpenGLShader.InjectDefines).
/// </summary>
internal sealed class OpenGLEnvironmentGenerator(IShaderFactory shaderFactory, IMeshFactory meshFactory)
{

    private static readonly (Vector3 Target, Vector3 Up)[] Faces =
    [
        (new Vector3(1, 0, 0), new Vector3(0, -1, 0)),
        (new Vector3(-1, 0, 0), new Vector3(0, -1, 0)),
        (new Vector3(0, 1, 0), new Vector3(0, 0, 1)),
        (new Vector3(0, -1, 0), new Vector3(0, 0, -1)),
        (new Vector3(0, 0, 1), new Vector3(0, -1, 0)),
        (new Vector3(0, 0, -1), new Vector3(0, -1, 0)),
    ];

    private IShader? _equirectToCubeShader;
    private IShader? _irradianceShader;
    private IShader? _prefilterShader;
    private IShader? _brdfLutShader;

    public EnvironmentMap Generate(string hdrPath)
    {
        var gl = SilkNetContext.GL;
        var prevFbo = (uint)gl.GetInteger(GLEnum.DrawFramebufferBinding);
        Span<int> prevViewport = stackalloc int[4];
        gl.GetInteger(GLEnum.Viewport, prevViewport);
        gl.Disable(EnableCap.DepthTest);
        gl.Disable(EnableCap.Blend);
        gl.Disable(EnableCap.CullFace);

        var fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        try
        {
            var equirect = UploadEquirect(hdrPath);
            uint envCube;
            try
            {
                envCube = AllocateCubemap(EnvironmentMapConstants.EnvironmentMapSize, withMips: true);
                _equirectToCubeShader ??= CreateShader("equirectToCube.frag");
                RenderToCubemap(_equirectToCubeShader, "u_EquirectMap", envCube, EnvironmentMapConstants.EnvironmentMapSize, mip: 0,
                    bindInput: () => Bind2D(equirect));
                gl.BindTexture(TextureTarget.TextureCubeMap, envCube);
                gl.GenerateMipmap(TextureTarget.TextureCubeMap);
            }
            finally
            {
                gl.DeleteTexture(equirect);
            }

            var irradianceCube = AllocateCubemap(EnvironmentMapConstants.IrradianceSize, withMips: false);
            _irradianceShader ??= CreateShader("irradianceConvolution.frag");
            RenderToCubemap(_irradianceShader, "u_EnvironmentMap", irradianceCube, EnvironmentMapConstants.IrradianceSize, mip: 0,
                bindInput: () => BindCube(envCube));

            var prefilteredCube = GeneratePrefilter(envCube);

            return new EnvironmentMap(
                new OpenGLTextureCube(envCube, EnvironmentMapConstants.EnvironmentMapSize),
                new OpenGLTextureCube(irradianceCube, EnvironmentMapConstants.IrradianceSize),
                new OpenGLTextureCube(prefilteredCube, EnvironmentMapConstants.PrefilterSize));
        }
        finally
        {
            gl.DeleteFramebuffer(fbo);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, prevFbo);
            gl.Viewport(prevViewport[0], prevViewport[1], (uint)prevViewport[2], (uint)prevViewport[3]);
            gl.BindTexture(TextureTarget.TextureCubeMap, 0);
            gl.BindTexture(TextureTarget.Texture2D, 0);
            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.Blend);
            gl.Enable(EnableCap.CullFace);
        }
    }

    private uint GeneratePrefilter(uint envCube)
    {
        var prefiltered = AllocateCubemap(EnvironmentMapConstants.PrefilterSize, withMips: true);
        _prefilterShader ??= shaderFactory.Create(
            ResolveHostShader("envCapture.vert"),
            ResolveHostShader("prefilterEnv.frag"),
            [new ShaderDefine("ENV_RESOLUTION", EnvironmentMapConstants.EnvironmentMapSize.ToString(System.Globalization.CultureInfo.InvariantCulture))]);
        for (var mip = 0; mip < EnvironmentMapConstants.PrefilterMips; mip++)
        {
            var mipSize = EnvironmentMapConstants.PrefilterSize >> mip;
            var roughness = mip / (float)(EnvironmentMapConstants.PrefilterMips - 1);
            _prefilterShader.Bind();
            _prefilterShader.SetFloat("u_Roughness", roughness);
            RenderToCubemap(_prefilterShader, "u_EnvironmentMap", prefiltered, mipSize, mip,
                bindInput: () => BindCube(envCube));
        }
        return prefiltered;
    }

    public unsafe Texture2D GenerateBrdfLut()
    {
        var gl = SilkNetContext.GL;
        var prevFbo = (uint)gl.GetInteger(GLEnum.DrawFramebufferBinding);
        Span<int> prevViewport = stackalloc int[4];
        gl.GetInteger(GLEnum.Viewport, prevViewport);
        gl.Disable(EnableCap.DepthTest);
        gl.Disable(EnableCap.Blend);

        var lut = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, lut);
        gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f,
            EnvironmentMapConstants.BrdfLutSize, EnvironmentMapConstants.BrdfLutSize, 0, PixelFormat.Rgba, PixelType.Float, null);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        var fbo = gl.GenFramebuffer();
        var emptyVao = gl.GenVertexArray();
        try
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, lut, 0);
            var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
                throw new InvalidOperationException($"IBL BRDF LUT FBO incomplete: {status}");
            gl.Viewport(0, 0, (uint)EnvironmentMapConstants.BrdfLutSize, (uint)EnvironmentMapConstants.BrdfLutSize);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            _brdfLutShader ??= shaderFactory.Create(ResolveHostShader("brdfLut.vert"), ResolveHostShader("brdfLut.frag"));
            _brdfLutShader.Bind();
            gl.BindVertexArray(emptyVao);
            gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
            gl.BindVertexArray(0);
            _brdfLutShader.Unbind();
            OpenGLDebug.CheckError(gl, "GenerateBrdfLut");
            return OpenGLTexture2D.CreateFromHandle(lut, EnvironmentMapConstants.BrdfLutSize, EnvironmentMapConstants.BrdfLutSize);
        }
        finally
        {
            gl.DeleteVertexArray(emptyVao);
            gl.DeleteFramebuffer(fbo);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, prevFbo);
            gl.Viewport(prevViewport[0], prevViewport[1], (uint)prevViewport[2], (uint)prevViewport[3]);
            gl.BindTexture(TextureTarget.Texture2D, 0);
            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.Blend);
        }
    }

    private IShader CreateShader(string fragFile) =>
        shaderFactory.Create(ResolveHostShader("envCapture.vert"), ResolveHostShader(fragFile));

    internal static string ResolveHostShader(string fileName) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "assets", "shaders", "OpenGL", fileName));

    private static void Bind2D(uint texture)
    {
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, texture);
    }

    private static void BindCube(uint texture)
    {
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, texture);
    }

    private static unsafe uint UploadEquirect(string hdrPath)
    {
        var gl = SilkNetContext.GL;
        var image = HdrImage.Load(hdrPath);

        var handle = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, handle);
        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        var rgba = new float[image.Width * image.Height * 4];
        for (var i = 0; i < image.Width * image.Height; i++)
        {
            rgba[i * 4] = image.Pixels[i * 3];
            rgba[i * 4 + 1] = image.Pixels[i * 3 + 1];
            rgba[i * 4 + 2] = image.Pixels[i * 3 + 2];
            rgba[i * 4 + 3] = 1f;
        }
        fixed (float* ptr = rgba)
        {
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f,
                (uint)image.Width, (uint)image.Height, 0, PixelFormat.Rgba, PixelType.Float, ptr);
        }
        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        OpenGLDebug.CheckError(gl, "UploadEquirect");
        return handle;
    }

    private static unsafe uint AllocateCubemap(int size, bool withMips)
    {
        var gl = SilkNetContext.GL;
        var handle = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.TextureCubeMap, handle);
        for (var face = 0; face < 6; face++)
        {
            gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + face, 0, InternalFormat.Rgba16f,
                (uint)size, (uint)size, 0, PixelFormat.Rgba, PixelType.Float, null);
        }
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            (int)(withMips ? TextureMinFilter.LinearMipmapLinear : TextureMinFilter.Linear));
        if (withMips)
            gl.GenerateMipmap(TextureTarget.TextureCubeMap);
        OpenGLDebug.CheckError(gl, "AllocateCubemap");
        return handle;
    }

    private unsafe void RenderToCubemap(IShader shader, string samplerName, uint cubemap, int size, int mip, Action bindInput)
    {
        var gl = SilkNetContext.GL;
        var cube = meshFactory.CreateCube();

        shader.Bind();
        shader.SetInt(samplerName, 0);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1f, 0.1f, 10f);

        gl.Viewport(0, 0, (uint)size, (uint)size);
        for (var face = 0; face < 6; face++)
        {
            var view = Matrix4x4.CreateLookAt(Vector3.Zero, Faces[face].Target, Faces[face].Up);
            shader.SetMat4("u_ViewProjection", view * projection);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + face, cubemap, mip);
            var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
                throw new InvalidOperationException($"IBL cubemap FBO incomplete: {status}");
            gl.Clear(ClearBufferMask.ColorBufferBit);
            bindInput();
            cube.Bind();
            gl.DrawElements(PrimitiveType.Triangles, (uint)cube.GetIndexCount(), DrawElementsType.UnsignedInt, null);
        }
        shader.Unbind();
        OpenGLDebug.CheckError(gl, $"RenderToCubemap({samplerName}, mip {mip})");
    }
}
