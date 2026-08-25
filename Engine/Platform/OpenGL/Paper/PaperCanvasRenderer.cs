using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Platform.SilkNet;
using Prowl.Quill;
using Prowl.Vector;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL.Paper;

internal sealed class PaperCanvasRenderer : ICanvasRenderer
{
    private static GL Gl =>
        SilkNetContext.GL ?? throw new InvalidOperationException("OpenGL context is not ready.");

    private bool _initialized;

    private uint _shaderProgram;
    private uint _vertexArrayObject;
    private uint _vertexBufferObject;
    private uint _elementBufferObject;
    private int _projectionLocation;
    private int _textureSamplerLocation;
    private int _fontTextureLoc;
    private int _scissorExtLoc;
    private int _scissorTransformLoc;
    private int _scissorTranslationLoc;
    private int _brushTransformLoc;
    private int _brushTranslationLoc;
    private int _textureTransformLoc;
    private int _textureTranslationLoc;
    private int _sdfPxRangeLoc;
    private float _sdfPxRange = 4f;
    private int _brushTypeLoc;
    private int _brushColor1Loc;
    private int _brushColor2Loc;
    private int _brushParamsLoc;
    private int _brushParams2Loc;
    private int _atlasTexelSizeLoc;
    private int _backdropFlipYLoc;
    private int _backdropTexLoc;
    private int _viewportSizeLoc;
    private int _backdropBlurAmountLoc;

    private Matrix4x4 _projection;
    private PaperTexture? _defaultTexture;
    private int _fbWidth;
    private int _fbHeight;
    private int _vertexBufferCapacity;
    private int _indexBufferCapacity;

    public bool SupportsBackdropBlur => false;

    public void Initialize(int width, int height)
    {
        SilkNetContext.EnsureCurrent();

        if (_initialized)
        {
            UpdateProjection(width, height);
            return;
        }

        InitializeShaders();

        _vertexArrayObject = Gl.GenVertexArray();
        _vertexBufferObject = Gl.GenBuffer();
        _elementBufferObject = Gl.GenBuffer();

        var texture = PaperTexture.CreateNew(1, 1);
        texture.SetData(new IntRect(0, 0, 1, 1), [255, 255, 255, 255]);
        _defaultTexture = texture;

        UpdateProjection(width, height);
        _initialized = true;
    }

    public void UpdateProjection(int width, int height)
    {
        _fbWidth = width;
        _fbHeight = height;
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
    }

    public void Cleanup()
    {
        if (!_initialized)
            return;

        SilkNetContext.EnsureCurrent();
        Gl.BindVertexArray(0);
        Gl.UseProgram(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        Gl.DeleteBuffer(_vertexBufferObject);
        Gl.DeleteBuffer(_elementBufferObject);
        Gl.DeleteVertexArray(_vertexArrayObject);
        if (_shaderProgram != 0)
            Gl.DeleteProgram(_shaderProgram);

        _defaultTexture?.Dispose();
        _defaultTexture = null;
        _shaderProgram = 0;
        _vertexArrayObject = 0;
        _vertexBufferObject = 0;
        _elementBufferObject = 0;
        _initialized = false;
    }

    private void InitializeShaders()
    {
        _shaderProgram = Gl.CreateProgram();

        var vertexShader = Gl.CreateShader(ShaderType.VertexShader);
        Gl.ShaderSource(vertexShader, CanvasShaders.Vertex);
        Gl.CompileShader(vertexShader);

        var fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
        Gl.ShaderSource(fragmentShader, CanvasShaders.Fragment);
        Gl.CompileShader(fragmentShader);

        Gl.AttachShader(_shaderProgram, vertexShader);
        Gl.AttachShader(_shaderProgram, fragmentShader);
        Gl.LinkProgram(_shaderProgram);

        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);

        _projectionLocation = Gl.GetUniformLocation(_shaderProgram, "projection");
        _textureSamplerLocation = Gl.GetUniformLocation(_shaderProgram, "texture0");
        _fontTextureLoc = Gl.GetUniformLocation(_shaderProgram, "fontTexture");
        _scissorTransformLoc = Gl.GetUniformLocation(_shaderProgram, "scissorTransform");
        _scissorExtLoc = Gl.GetUniformLocation(_shaderProgram, "scissorExt");
        _scissorTranslationLoc = Gl.GetUniformLocation(_shaderProgram, "scissorTranslation");
        _brushTransformLoc = Gl.GetUniformLocation(_shaderProgram, "brushTransform");
        _brushTranslationLoc = Gl.GetUniformLocation(_shaderProgram, "brushTranslation");
        _brushTypeLoc = Gl.GetUniformLocation(_shaderProgram, "brushType");
        _brushColor1Loc = Gl.GetUniformLocation(_shaderProgram, "brushColor1");
        _brushColor2Loc = Gl.GetUniformLocation(_shaderProgram, "brushColor2");
        _brushParamsLoc = Gl.GetUniformLocation(_shaderProgram, "brushParams");
        _brushParams2Loc = Gl.GetUniformLocation(_shaderProgram, "brushParams2");
        _textureTransformLoc = Gl.GetUniformLocation(_shaderProgram, "textureTransform");
        _textureTranslationLoc = Gl.GetUniformLocation(_shaderProgram, "textureTranslation");
        _sdfPxRangeLoc = Gl.GetUniformLocation(_shaderProgram, "sdfPxRange");
        _atlasTexelSizeLoc = Gl.GetUniformLocation(_shaderProgram, "atlasTexelSize");
        _backdropFlipYLoc = Gl.GetUniformLocation(_shaderProgram, "backdropFlipY");
        _backdropTexLoc = Gl.GetUniformLocation(_shaderProgram, "backdropTexture");
        _viewportSizeLoc = Gl.GetUniformLocation(_shaderProgram, "viewportSize");
        _backdropBlurAmountLoc = Gl.GetUniformLocation(_shaderProgram, "backdropBlurAmount");
    }

    private static Matrix4x4 ToMatrix4x4(Float4x4 mat) => new(
        mat[0, 0], mat[1, 0], mat[2, 0], mat[3, 0],
        mat[0, 1], mat[1, 1], mat[2, 1], mat[3, 1],
        mat[0, 2], mat[1, 2], mat[2, 2], mat[3, 2],
        mat[0, 3], mat[1, 3], mat[2, 3], mat[3, 3]
    );

    private static Vector4 ToVec4(Color32 color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    private static void SetMat4(int location, Matrix4x4 matrix, bool transpose)
    {
        if (location < 0) return;
        var span = MemoryMarshal.CreateReadOnlySpan(ref matrix.M11, 16);
        Gl.UniformMatrix4(location, transpose, span);
    }

    private static void SetCustomUniforms(uint program, ShaderUniforms uniforms)
    {
        foreach (var kvp in uniforms.Values)
        {
            var loc = Gl.GetUniformLocation(program, kvp.Key);
            if (loc < 0) continue;

            switch (kvp.Value)
            {
                case float f:
                    Gl.Uniform1(loc, f);
                    break;
                case int i:
                    Gl.Uniform1(loc, i);
                    break;
                case Float2 v2:
                    Gl.Uniform2(loc, v2.X, v2.Y);
                    break;
                case Float3 v3:
                    Gl.Uniform3(loc, v3.X, v3.Y, v3.Z);
                    break;
                case Float4 v4:
                    Gl.Uniform4(loc, v4.X, v4.Y, v4.Z, v4.W);
                    break;
                case Float4x4 mat:
                    SetMat4(loc, ToMatrix4x4(mat), transpose: false);
                    break;
            }
        }
    }

    public object CreateTexture(uint width, uint height) => PaperTexture.CreateNew(width, height);

    public Int2 GetTextureSize(object texture)
    {
        if (texture is not PaperTexture paperTexture)
            throw new ArgumentException("Invalid texture type");

        return new Int2((int)paperTexture.Width, (int)paperTexture.Height);
    }

    public void SetTextureData(object texture, IntRect bounds, byte[] data)
    {
        if (texture is not PaperTexture paperTexture)
            throw new ArgumentException("Invalid texture type");
        paperTexture.SetData(bounds, data);
    }

    private static unsafe void UploadStream<T>(BufferTargetARB target, ref int capacity, int sizeInBytes, T[] data)
        where T : unmanaged
    {
        if (sizeInBytes > capacity)
            capacity = System.Math.Max(sizeInBytes, capacity == 0 ? 64 * 1024 : capacity * 2);

        Gl.BufferData(target, (nuint)capacity, null, BufferUsageARB.StreamDraw);
        fixed (T* p = data)
            Gl.BufferSubData(target, 0, (nuint)sizeInBytes, p);
    }

    public void RenderCalls(Canvas canvas, IReadOnlyList<DrawCall> drawCalls)
    {
        _sdfPxRange = canvas.Text.FontEngine.DistanceRange;

        if (drawCalls.Count == 0)
            return;

        Gl.Disable(EnableCap.DepthTest);
        Gl.Enable(EnableCap.Blend);
        Gl.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

        Gl.UseProgram(_shaderProgram);
        SetMat4(_projectionLocation, _projection, transpose: true);

        Gl.BindVertexArray(_vertexArrayObject);

        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBufferObject);
        UploadStream(BufferTargetARB.ArrayBuffer, ref _vertexBufferCapacity, canvas.VertexCount * Vertex.SizeInBytes,
            canvas.VertexBuffer);

        var stride = Vertex.SizeInBytes;

        Gl.EnableVertexAttribArray(0);
        unsafe
        {
            Gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)0);
            Gl.EnableVertexAttribArray(1);
            Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)8);
            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, (uint)stride, (void*)16);
        }

        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _elementBufferObject);
        UploadStream(BufferTargetARB.ElementArrayBuffer, ref _indexBufferCapacity, canvas.IndexCount * sizeof(uint),
            canvas.IndexBuffer);

        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.Uniform1(_textureSamplerLocation, 0);
        Gl.Uniform1(_fontTextureLoc, 1);

        var indexOffset = 0;
        foreach (var drawCall in drawCalls)
        {
            (drawCall.FontAtlas as PaperTexture ?? _defaultTexture!).Use(TextureUnit.Texture1);
            (drawCall.Texture as PaperTexture ?? _defaultTexture!).Use(TextureUnit.Texture0);

            if (drawCall.Shader is int customProgram)
            {
                Gl.UseProgram((uint)customProgram);

                var projLoc = Gl.GetUniformLocation((uint)customProgram, "projection");
                if (projLoc >= 0)
                    SetMat4(projLoc, _projection, transpose: false);

                var texLoc = Gl.GetUniformLocation((uint)customProgram, "texture0");
                if (texLoc >= 0)
                    Gl.Uniform1(texLoc, 0);

                if (drawCall.ShaderUniforms != null)
                    SetCustomUniforms((uint)customProgram, drawCall.ShaderUniforms);
            }
            else
            {
                Gl.UseProgram(_shaderProgram);
                SetMat4(_projectionLocation, _projection, transpose: true);

                var fbScale = canvas.FramebufferScale;

                drawCall.GetScissor(fbScale, out var scissorXf, out var scissorT, out var extent);
                Gl.Uniform4(_scissorTransformLoc, scissorXf.X, scissorXf.Y, scissorXf.Z,
                    scissorXf.W);
                Gl.Uniform2(_scissorTranslationLoc, scissorT.X, scissorT.Y);
                Gl.Uniform2(_scissorExtLoc, extent.X, extent.Y);

                drawCall.GetBrushTransform(fbScale, out var brushXf, out var brushT);
                Gl.Uniform4(_brushTransformLoc, brushXf.X, brushXf.Y, brushXf.Z,
                    brushXf.W);
                Gl.Uniform2(_brushTranslationLoc, brushT.X, brushT.Y);
                Gl.Uniform1(_brushTypeLoc, (int)drawCall.Brush.Type);
                Gl.Uniform4(_brushColor1Loc, ToVec4(drawCall.Brush.Color1));
                Gl.Uniform4(_brushColor2Loc, ToVec4(drawCall.Brush.Color2));
                Gl.Uniform4(_brushParamsLoc, drawCall.Brush.Point1.X, drawCall.Brush.Point1.Y,
                    drawCall.Brush.Point2.X, drawCall.Brush.Point2.Y);
                Gl.Uniform2(_brushParams2Loc, drawCall.Brush.CornerRadii, drawCall.Brush.Feather);

                drawCall.GetTextureTransform(fbScale, out var texXf, out var texT);
                Gl.Uniform4(_textureTransformLoc, texXf.X, texXf.Y, texXf.Z, texXf.W);
                Gl.Uniform2(_textureTranslationLoc, texT.X, texT.Y);
                Gl.Uniform1(_sdfPxRangeLoc, _sdfPxRange);

                Gl.Uniform2(_viewportSizeLoc, _fbWidth, (float)_fbHeight);
                Gl.Uniform1(_backdropTexLoc, 3);

                var atlas = drawCall.FontAtlas as PaperTexture ?? _defaultTexture!;
                Gl.Uniform2(_atlasTexelSizeLoc, atlas.Width > 0 ? 1f / atlas.Width : 0f,
                    atlas.Height > 0 ? 1f / atlas.Height : 0f);
                Gl.Uniform1(_backdropBlurAmountLoc, 0f);
                Gl.Uniform1(_backdropFlipYLoc, 1);
            }

            unsafe
            {
                Gl.DrawElements(PrimitiveType.Triangles, (uint)drawCall.ElementCount, DrawElementsType.UnsignedInt,
                    (void*)(indexOffset * sizeof(uint)));
            }

            indexOffset += drawCall.ElementCount;
        }

        Gl.BindVertexArray(0);
        Gl.UseProgram(0);
        Gl.Enable(EnableCap.DepthTest);
        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    public void Dispose() => Cleanup();
}
