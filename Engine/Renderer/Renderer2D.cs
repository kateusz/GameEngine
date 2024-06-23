using Engine.Renderer.Buffers;
using Engine.Renderer.Cameras;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;
using System.Numerics;
using Engine.Math;
using TextureFactory = Engine.Renderer.Textures.TextureFactory;

namespace Engine.Renderer;

public class Renderer2D
{
    private static Renderer2D? _instance;

    public static Renderer2D Instance => _instance ??= new Renderer2D();

    private Renderer2DData _data;

    public void Init()
    {
        _data = new Renderer2DData();

        _data.QuadVertexArray = VertexArrayFactory.Create();
        uint quadVertexSize = 36;

        var layout = new BufferLayout(new[]
        {
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float4, "a_Color"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
        });
        _data.QuadVertexBuffer = VertexBufferFactory.Create(Renderer2DData.MaxVertices * quadVertexSize);
        _data.QuadVertexBuffer.SetLayout(layout);

        _data.QuadVertexArray.AddVertexBuffer(_data.QuadVertexBuffer);
        _data.QuadVertexBufferBase = new List<QuadVertex>(Renderer2DData.MaxVertices);

        var quadIndices = new uint[Renderer2DData.MaxIndices];

        uint offset = 0;
        for (uint i = 0; i < Renderer2DData.MaxIndices; i += 6)
        {
            quadIndices[i + 0] = offset + 0;
            quadIndices[i + 1] = offset + 1;
            quadIndices[i + 2] = offset + 2;

            quadIndices[i + 3] = offset + 2;
            quadIndices[i + 4] = offset + 3;
            quadIndices[i + 5] = offset + 0;

            offset += 4;
        }

        var indexBuffer = IndexBufferFactory.Create(quadIndices, Renderer2DData.MaxIndices);
        _data.QuadVertexArray.SetIndexBuffer(indexBuffer);

        _data.WhiteTexture = TextureFactory.Create(1, 1);
        uint whiteTextureData = 0xffffffff;
        _data.WhiteTexture.SetData(whiteTextureData, sizeof(uint));

        _data.TextureShader = ShaderFactory.Create("assets/shaders/opengl/textureShader.vert",
            "assets/shaders/opengl/textureShader.frag");
        _data.TextureShader.Bind();

        _data.QuadVertexPositions.Add(new Vector4(-0.5f, -0.5f, 0.0f, 1.0f));
        _data.QuadVertexPositions.Add(new Vector4(0.5f, -0.5f, 0.0f, 1.0f));
        _data.QuadVertexPositions.Add(new Vector4(0.5f, 0.5f, 0.0f, 1.0f));
        _data.QuadVertexPositions.Add(new Vector4(-0.5f, 0.5f, 0.0f, 1.0f));
        
        //_data.TextureShader.SetInt("u_Texture", 0);
    }

    public void Shutdown()
    {
    }

    public void BeginScene(OrthographicCamera camera)
    {
        _data.TextureShader.Bind();
        _data.TextureShader.SetMat4("u_ViewProjection", camera.ViewProjectionMatrix);
        _data.QuadVertexBufferBase = [];
        _data.QuadIndexBufferCount = 0;
        _data.CurrentVertexBufferIndex = 0;
    }

    public void EndScene()
    {
        uint dataSize = 0;
        for (int i = 0; i < _data.CurrentVertexBufferIndex; i++)
        {
            dataSize += QuadVertex.GetSize();
        }

        // upload data to GPU
        _data.QuadVertexBuffer.SetData(_data.QuadVertexBufferBase.ToArray(), dataSize);

        Flush();
    }

    private void Flush()
    {
        if (_data.QuadIndexBufferCount == 0)
            return; // Nothing to draw

        RendererCommand.DrawIndexed(_data.QuadVertexArray, _data.QuadIndexBufferCount);
    }

    public void DrawQuad(Vector2 position, Vector2 size, Vector4 color)
    {
        Matrix4x4 transform = Matrix4x4.CreateTranslation(new Vector3(position, 0.0f)) *
                              Matrix4x4.CreateScale(new Vector3(size.X, size.Y, 1.0f));

        var quadVertexCount = 4;
        Vector2[] textureCoords =
        [
            new(0.0f, 0.0f),
            new(1.0f, 0.0f),
            new(1.0f, 1.0f),
            new(0.0f, 1.0f)
        ];

        for (var i = 0; i < quadVertexCount; i++)
        {
            var vector3 = new Vector3(_data.QuadVertexPositions[i].X, _data.QuadVertexPositions[i].Y,
                _data.QuadVertexPositions[i].Z);

            _data.QuadVertexBufferBase.Add(new QuadVertex
            {
                Position = Vector3.Transform(vector3, transform),
                Color = color,
                TexCoord = textureCoords[i],
                //TexIndex = textureIndex;
                //TilingFactor = tilingFactor;
            });
        }


        _data.CurrentVertexBufferIndex++;

        _data.QuadIndexBufferCount += 6;
    }

    public void DrawQuad(Vector2 position, Vector2 size, Texture2D texture, float tilingFactor = 1.0f,
        Vector4? tintColor = null)
    {
        DrawQuad(new Vector3(position.X, position.Y, 0.0f), size, texture, tilingFactor, tintColor);
    }

    public void DrawQuad(Vector3 position, Vector2 size, Vector4 color)
    {
        _data.TextureShader.SetFloat4("u_Color", color);

        // Tiling factor doesn't apply to flat color shader
        _data.TextureShader.SetFloat("u_TilingFactor", 1.0f);
        _data.WhiteTexture.Bind();

        var positionTranslated = Matrix4x4.CreateTranslation(position.X, position.Y, 0);
        var scale = Matrix4x4.CreateScale(size.X, size.Y, 1.0f);
        var transform = Matrix4x4.Identity * positionTranslated * scale; /* *rotation */
        _data.TextureShader.SetMat4("u_Transform", transform);

        _data.QuadVertexArray.Bind();
        RendererCommand.DrawIndexed(_data.QuadVertexArray);
    }


    public void DrawQuad(Vector3 position, Vector2 size, Texture2D texture, float tilingFactor = 1.0f,
        Vector4? tintColor = null)
    {
        tintColor ??= Vector4.One;

        _data.TextureShader.SetFloat4("u_Color", tintColor.Value);
        _data.TextureShader.SetFloat("u_TilingFactor", tilingFactor);
        texture.Bind();

        var positionTranslated = Matrix4x4.CreateTranslation(position);
        var scale = Matrix4x4.CreateScale(size.X, size.Y, 1.0f);
        var transform = Matrix4x4.Identity * positionTranslated * scale; /* *rotation */
        _data.TextureShader.SetMat4("u_Transform", transform);

        _data.QuadVertexArray.Bind();
        RendererCommand.DrawIndexed(_data.QuadVertexArray);
    }

    public void DrawRotatedQuad(Vector2 position, Vector2 size, float rotation, Vector4 color)
    {
        DrawRotatedQuad(new Vector3(position.X, position.Y, 0.0f), size, rotation, color);
    }

    public void DrawRotatedQuad(Vector3 position, Vector2 size, float rotation, Vector4 color)
    {
        _data.TextureShader.SetFloat4("u_Color", color);
        _data.TextureShader.SetFloat("u_TilingFactor", 1.0f);
        _data.WhiteTexture.Bind();

        var transform = Matrix4x4.CreateTranslation(position) *
                        Matrix4x4.CreateRotationZ(MathHelpers.ToRadians(rotation)) *
                        Matrix4x4.CreateScale(size.X, size.Y, 1.0f);

        _data.TextureShader.SetMat4("u_Transform", transform);

        _data.QuadVertexArray.Bind();
        RendererCommand.DrawIndexed(_data.QuadVertexArray);
    }

    public void DrawRotatedQuad(Vector2 position, Vector2 size, float rotation, Texture2D texture, float tilingFactor,
        Vector4? tintColor = null)
    {
        DrawRotatedQuad(new Vector3(position.X, position.Y, 0.0f), size, rotation, texture, tilingFactor, tintColor);
    }

    public void DrawRotatedQuad(Vector3 position, Vector2 size, float rotation, Texture2D texture, float tilingFactor,
        Vector4? tintColor = null)
    {
        tintColor ??= Vector4.One;

        _data.TextureShader.SetFloat4("u_Color", tintColor.Value);
        _data.TextureShader.SetFloat("u_TilingFactor", tilingFactor);
        texture.Bind();

        var transform = Matrix4x4.CreateTranslation(position) *
                        Matrix4x4.CreateRotationZ(MathHelpers.ToRadians(rotation)) *
                        Matrix4x4.CreateScale(size.X, size.Y, 1.0f);

        _data.TextureShader.SetMat4("u_Transform", transform);

        _data.QuadVertexArray.Bind();
        RendererCommand.DrawIndexed(_data.QuadVertexArray);
    }
}