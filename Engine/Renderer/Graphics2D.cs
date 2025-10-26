using Engine.Renderer.Buffers;
using Engine.Renderer.Cameras;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;
using System.Numerics;
using Engine.Math;
using Engine.Platform;
using Engine.Scene.Components;
using TextureFactory = Engine.Renderer.Textures.TextureFactory;

namespace Engine.Renderer;

public class Graphics2D : IGraphics2D
{
    private readonly IRendererAPI _rendererApi = RendererApiFactory.Create();
    private Renderer2DData _data = new();
    private static readonly Vector2[] DefaultTextureCoords;
    private bool _disposed;

    static Graphics2D()
    {
        DefaultTextureCoords =
        [
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 1.0f)
        ];
    }

    public void Init()
    {
        _data = new Renderer2DData
        {
            QuadVertexArray = VertexArrayFactory.Create(),
            //CameraUniformBuffer = UniformBufferFactory.Create((uint)CameraData.GetSize(), 0),
            Stats = new Statistics(),
            //CameraBuffer = new CameraData(),
            LineVertexArray = VertexArrayFactory.Create()
        };

        InitBuffers();
        InitWhiteTexture();
        InitShaders();
        InitQuadVertexPositions();
    }

    public void Shutdown()
    {
        // Deprecated: Use Dispose() instead
        Dispose();
    }
    
    public void BeginScene(OrthographicCamera camera)
    {
        _data.QuadShader.Bind();
        _data.QuadShader.SetMat4("u_ViewProjection", camera.ViewProjectionMatrix);
        
        _data.LineShader.Bind();
        _data.LineShader.SetMat4("u_ViewProjection", camera.ViewProjectionMatrix);
        
        StartBatch();
    }

    public void BeginScene(Camera camera, Matrix4x4 transform)
    {
        _ = Matrix4x4.Invert(transform, out var transformInverted);
        Matrix4x4? viewProj = null;

        if (OSInfo.IsWindows)
        {
            viewProj =  transformInverted * camera.Projection;
        }
        else if (OSInfo.IsMacOS)
        {
            viewProj = camera.Projection * transformInverted;
        }
        else
            throw new InvalidOperationException("Unsupported OS version!");

        //_data.CameraBuffer.ViewProjection = camera.Projection * transformInverted;
        //_data.CameraUniformBuffer.SetData(_data.CameraBuffer, CameraData.GetSize());
        _data.QuadShader.Bind();
        _data.QuadShader.SetMat4("u_ViewProjection", viewProj.Value);
        
        _data.LineShader.Bind();
        _data.LineShader.SetMat4("u_ViewProjection", viewProj.Value);

        StartBatch();
    }

    public void EndScene()
    {
        Flush();
    }

    public void DrawQuad(Vector2 position, Vector2 size, Vector4 color)
    {
        DrawQuad(new Vector3(position.X, position.Y, 1.0f), size, rotation: 0, texture: null, textureCoords: DefaultTextureCoords, tilingFactor: 1.0f,
            tintColor: color);
    }

    public void DrawQuad(Vector3 position, Vector2 size, Vector4 color)
    {
        DrawQuad(position with { Z = 0.0f }, size, rotation: 0, texture: null, textureCoords: DefaultTextureCoords, tilingFactor: 1.0f, tintColor: color);
    }
    
    public void DrawQuad(Vector3 position, Vector2 size, float rotation, SubTexture2D subTexture)
    {
        var transform = CalculateTransform(position, size, rotation);

        var (texture, textureCoords) = subTexture;
        const float tilingFactor = 1.0f;
        var tintColor = Vector4.One;

        DrawQuad(transform, texture, textureCoords, tilingFactor, tintColor);
    }

    public void DrawQuad(Vector2 position, Vector2 size, Texture2D texture, float tilingFactor = 1.0f, Vector4? tintColor = null)
    {
        DrawQuad(new Vector3(position.X, position.Y, 0.0f), size, rotation: 0, texture, DefaultTextureCoords, tilingFactor, tintColor);
    }

    public void DrawQuad(Vector3 position, Vector2 size, Texture2D? texture, Vector2[] textureCoords, float tilingFactor = 1.0f, Vector4? tintColor = null)
    {
        DrawQuad(position with { Z = 0.0f }, size, rotation: 0, texture, textureCoords, tilingFactor, tintColor);
    }

    public void DrawQuad(Vector3 position, Vector2 size, float rotation, Texture2D? texture, Vector2[] textureCoords, float tilingFactor = 1.0f, Vector4? tintColor = null)
    {
        var transform = CalculateTransform(position, size, rotation);
        DrawQuad(transform, texture, textureCoords, tilingFactor, tintColor);
    }

    public void DrawQuad(Matrix4x4 transform, Vector4 color, int entityId = -1)
    {
        DrawQuad(transform, texture: null, textureCoords: DefaultTextureCoords, tilingFactor: 1.0f, color, entityId);
    }

    /// <summary>
    /// Draws a textured or colored quad
    /// </summary>
    /// <param name="transform">
    /// The transformation matrix applied to the quad's vertices. This matrix defines the position, rotation, and scaling of the quad.
    /// </param>
    /// <param name="texture">
    /// The texture applied to the quad. If <c>null</c>, the quad will be drawn with a solid color using the specified <paramref name="tintColor"/>.
    /// </param>
    /// <param name="textureCoords">tbd</param>
    /// <param name="tilingFactor">
    /// The tiling factor for the texture coordinates. This value determines how many times the texture repeats over the quad. Default is 1.0f (no tiling).
    /// </param>
    /// <param name="tintColor">
    /// The color tint applied to the quad. If <c>null</c>, the default value is <see cref="Vector4.One"/>, representing white (no tint).
    /// </param>
    /// <param name="entityId">
    /// The ID of the entity associated with the quad. Used for identifying the quad in entity-based systems. Default is -1 (no entity).
    /// </param>
    /// <remarks>
    /// This method handles batching of quads and may trigger a new batch if the current batch reaches the maximum allowed indices or texture slots.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the maximum number of texture slots is exceeded when a new texture is added.
    /// </exception>
    public void DrawQuad(Matrix4x4 transform, Texture2D? texture, Vector2[] textureCoords, float tilingFactor = 1.0f, Vector4? tintColor = null, int entityId = -1)
    {
        tintColor ??= Vector4.One;

        if (_data.QuadIndexBufferCount >= Renderer2DData.MaxIndices)
            NextBatch();

        const int quadVertexCount = RenderingConstants.QuadVertexCount;

        var textureIndex = 0.0f;
        if (texture is not null)
        {
            for (var i = 1; i < _data.TextureSlotIndex; i++)
            {
                if (ReferenceEquals(_data.TextureSlots[i], texture))
                {
                    textureIndex = i;
                    break;
                }
            }

            if (System.Math.Abs(textureIndex) < float.Epsilon)
            {
                if (_data.TextureSlotIndex >= Renderer2DData.MaxTextureSlots)
                    NextBatch();

                textureIndex = _data.TextureSlotIndex;
                _data.TextureSlots[_data.TextureSlotIndex] = texture;
                _data.TextureSlotIndex++;
            }
        }

        for (var i = 0; i < quadVertexCount; i++)
        {
            var vector3 = new Vector3(_data.QuadVertexPositions[i].X, _data.QuadVertexPositions[i].Y,
                _data.QuadVertexPositions[i].Z);

            _data.QuadVertexBufferBase[_data.CurrentVertexBufferIndex] = new QuadVertex
            {
                Position = Vector3.Transform(vector3, transform),
                Color = tintColor.Value,
                TexCoord = textureCoords[i],
                TexIndex = textureIndex,
                TilingFactor = tilingFactor,
                EntityId = entityId
            };

            _data.CurrentVertexBufferIndex++;
        }

        _data.QuadIndexBufferCount += RenderingConstants.QuadIndexCount;
        _data.Stats.QuadCount++;
    }
    
    public void DrawSprite(Matrix4x4 transform, SpriteRendererComponent src, int entityId)
    {
        if (src.Texture is not null)
            DrawQuad(transform, src.Texture, DefaultTextureCoords, src.TilingFactor, src.Color, entityId);
        else
            DrawQuad(transform, src.Color, entityId);
    }
    
    public void DrawLine(Vector3 p0, Vector3 p1, Vector4 color, int entityId)
    {
        // Check if we need to flush before adding 2 more vertices
        if (_data.CurrentLineVertexBufferIndex + 2 >= Renderer2DData.MaxVertices)
            NextBatch();
        
        _data.LineVertexBufferBase[_data.CurrentLineVertexBufferIndex] = new LineVertex
        {
            Position = p0,
            Color = color,
            EntityId = entityId
        };

        _data.CurrentLineVertexBufferIndex++;

        _data.LineVertexBufferBase[_data.CurrentLineVertexBufferIndex] = new LineVertex
        {
            Position = p1,
            Color = color,
            EntityId = entityId
        };

        _data.CurrentLineVertexBufferIndex++;
        _data.LineVertexCount += 2;
    }
    
    public void DrawRect(Vector3 position, Vector2 size, Vector4 color, int entityId)
    {
        // Calculate the four corners of the rectangle
        Vector3 p0 = new Vector3(position.X - size.X * 0.5f, position.Y - size.Y * 0.5f, position.Z);
        Vector3 p1 = new Vector3(position.X + size.X * 0.5f, position.Y - size.Y * 0.5f, position.Z);
        Vector3 p2 = new Vector3(position.X + size.X * 0.5f, position.Y + size.Y * 0.5f, position.Z);
        Vector3 p3 = new Vector3(position.X - size.X * 0.5f, position.Y + size.Y * 0.5f, position.Z);

        // Draw the rectangle's edges
        DrawLine(p0, p1, color, entityId);
        DrawLine(p1, p2, color, entityId);
        DrawLine(p2, p3, color, entityId);
        DrawLine(p3, p0, color, entityId);
    }
    
    public void DrawRect(Matrix4x4 transform, Vector4 color, int entityId)
    {
        Vector3[] lineVertices = new Vector3[RenderingConstants.QuadVertexCount];
        for (var i = 0; i < RenderingConstants.QuadVertexCount; i++)
        {
            var vector3 = new Vector3(_data.QuadVertexPositions[i].X, _data.QuadVertexPositions[i].Y,
                _data.QuadVertexPositions[i].Z);

            lineVertices[i] = Vector3.Transform(vector3, transform);
        }

        DrawLine(lineVertices[0], lineVertices[1], color, entityId);
        DrawLine(lineVertices[1], lineVertices[2], color, entityId);
        DrawLine(lineVertices[2], lineVertices[3], color, entityId);
        DrawLine(lineVertices[3], lineVertices[0], color, entityId);
    }
    
    private void StartBatch()
    {
        Array.Clear(_data.QuadVertexBufferBase, 0, _data.QuadVertexBufferBase.Length);
        _data.QuadIndexBufferCount = 0;
        _data.CurrentVertexBufferIndex = 0;
        _data.TextureSlotIndex = 1;

        Array.Clear(_data.LineVertexBufferBase, 0, _data.LineVertexBufferBase.Length);
        _data.LineVertexCount = 0;
        _data.CurrentLineVertexBufferIndex = 0;
    }
    
    private void NextBatch()
    {
        Flush();
        StartBatch();
    }
    
    private void Flush()
    {
        if (_data.QuadIndexBufferCount > 0)
        {
            // Calculate actual data size (already known from index)
            int vertexCount = _data.CurrentVertexBufferIndex;
            int dataSize = vertexCount * QuadVertex.GetSize();

            // Make sure we're using the quad shader
            _data.QuadShader.Bind();

            // Explicitly bind the quad vertex array
            _data.QuadVertexArray.Bind();

            // Upload only used portion to GPU
            Span<QuadVertex> usedVertices = _data.QuadVertexBufferBase.AsSpan(0, vertexCount);
            _data.QuadVertexBuffer.SetData(usedVertices, dataSize);

            // Bind textures
            for (var i = 0; i < _data.TextureSlotIndex; i++)
                _data.TextureSlots[i].Bind(i);

            //_data.TextureShader.Bind();
            _rendererApi.DrawIndexed(_data.QuadVertexArray, _data.QuadIndexBufferCount);
            _data.Stats.DrawCalls++;
        }

        if (_data.LineVertexCount > 0)
        {
            // Switch to line shader
            _data.LineShader.Bind();

            // Explicitly bind line vertex array
            _data.LineVertexArray.Bind();

            // Calculate actual data size (already known from index)
            int lineVertexCount = _data.CurrentLineVertexBufferIndex;
            int dataSize = lineVertexCount * LineVertex.GetSize();

            // Upload only used portion to GPU
            Span<LineVertex> usedLineVertices = _data.LineVertexBufferBase.AsSpan(0, lineVertexCount);
            _data.LineVertexBuffer.SetData(usedLineVertices, dataSize);

            //_data.TextureShader.Bind();
            _rendererApi.SetLineWidth(Renderer2DData.LineWidth);
            _rendererApi.DrawLines(_data.LineVertexArray, _data.LineVertexCount);
            _data.Stats.DrawCalls++;
        }

    }

    private void InitBuffers()
    {
        var quadVertexSize = QuadVertex.GetSize();
        var layout = new BufferLayout([
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float4, "a_Color"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
            new BufferElement(ShaderDataType.Float, "a_TexIndex"),
            new BufferElement(ShaderDataType.Float, "a_TilingFactor"),
            new BufferElement(ShaderDataType.Int, "a_EntityID")
        ]);

        _data.QuadVertexBuffer = VertexBufferFactory.Create((uint)(Renderer2DData.MaxVertices * quadVertexSize));
        _data.QuadVertexBuffer.SetLayout(layout);
        _data.QuadVertexArray.AddVertexBuffer(_data.QuadVertexBuffer);
        _data.QuadVertexBufferBase = new QuadVertex[Renderer2DData.MaxVertices];

        var quadIndices = CreateQuadIndices();
        var indexBuffer = IndexBufferFactory.Create(quadIndices, Renderer2DData.MaxIndices);
        _data.QuadVertexArray.SetIndexBuffer(indexBuffer);
        
        var lineVertexSize = LineVertex.GetSize();
        var lineLayout = new BufferLayout([
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float4, "a_Color"),
            new BufferElement(ShaderDataType.Int, "a_EntityID")
        ]);
        
        _data.LineVertexBuffer = VertexBufferFactory.Create((uint)(Renderer2DData.MaxVertices * lineVertexSize));
        _data.LineVertexBuffer.SetLayout(lineLayout);
        _data.LineVertexArray.AddVertexBuffer(_data.LineVertexBuffer);
        _data.LineVertexBufferBase = new LineVertex[Renderer2DData.MaxVertices];
    }

    private void InitWhiteTexture()
    {
        _data.WhiteTexture = TextureFactory.GetWhiteTexture();
        _data.TextureSlots[0] = _data.WhiteTexture;
    }

    private void InitShaders()
    {
        var samplers = new int[Renderer2DData.MaxTextureSlots];
        for (var i = 0; i < Renderer2DData.MaxTextureSlots; i++)
            samplers[i] = i;

        _data.QuadShader = ShaderFactory.Create("assets/shaders/opengl/textureShader.vert",
            "assets/shaders/opengl/textureShader.frag");
        _data.QuadShader.Bind();
        _data.QuadShader.SetIntArray("u_Textures[0]", samplers, Renderer2DData.MaxTextureSlots);

        _data.LineShader = ShaderFactory.Create("assets/shaders/opengl/lineShader.vert",
            "assets/shaders/opengl/lineShader.frag");
        _data.LineShader.Bind();
    }

    private void InitQuadVertexPositions()
    {
        _data.QuadVertexPositions[0] = new Vector4(-0.5f, -0.5f, 0.0f, 1.0f);
        _data.QuadVertexPositions[1] = new Vector4(0.5f, -0.5f, 0.0f, 1.0f);
        _data.QuadVertexPositions[2] = new Vector4(0.5f, 0.5f, 0.0f, 1.0f);
        _data.QuadVertexPositions[3] = new Vector4(-0.5f, 0.5f, 0.0f, 1.0f);
    }

    private static uint[] CreateQuadIndices()
    {
        var quadIndices = new uint[Renderer2DData.MaxIndices];

        uint offset = 0;
        for (uint i = 0; i < Renderer2DData.MaxIndices; i += RenderingConstants.QuadIndexCount)
        {
            // first triangle
            quadIndices[i + 0] = offset + 0;
            quadIndices[i + 1] = offset + 1;
            quadIndices[i + 2] = offset + 2;

            // second triangle
            quadIndices[i + 3] = offset + 2;
            quadIndices[i + 4] = offset + 3;
            quadIndices[i + 5] = offset + 0;

            offset += RenderingConstants.QuadVertexCount;
        }

        return quadIndices;
    }
    
    private static Matrix4x4 CalculateTransform(Vector3 position, Vector2 size, float rotation)
    {
        var transform = Matrix4x4.CreateTranslation(position);

        if (System.Math.Abs(rotation) > float.Epsilon)
        {
            transform *= Matrix4x4.CreateRotationZ(MathHelpers.DegreesToRadians(rotation));
        }

        transform *= Matrix4x4.CreateScale(size.X, size.Y, 1.0f);
        return transform;
    }

    #region stats

    public void ResetStats()
    {
        _data.Stats.QuadCount = 0;
        _data.Stats.DrawCalls = 0;
    }
    
    public Statistics GetStats()
    {
        return _data.Stats;
    }

    #endregion

    public void SetClearColor(Vector4 color) => _rendererApi.SetClearColor(color);

    public void Clear() => _rendererApi.Clear();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            _data.QuadShader?.Dispose();
            _data.LineShader?.Dispose();
            _data.QuadVertexArray?.Dispose();
            _data.LineVertexArray?.Dispose();
            _data.QuadVertexBuffer?.Dispose();
            _data.LineVertexBuffer?.Dispose();
            _data.WhiteTexture?.Dispose();

            // Dispose textures in texture slots
            foreach (var texture in _data.TextureSlots)
            {
                texture?.Dispose();
            }
        }

        _disposed = true;
    }
}