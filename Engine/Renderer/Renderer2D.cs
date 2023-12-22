using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Renderer;

public class Renderer2D
{
    private static Renderer2D? _instance;

    public static Renderer2D Instance => _instance ??= new Renderer2D();

    private Renderer2DStorage _data;
    private OrthographicCameraController _cameraController;

    public void Init()
    {
        var textureShader = ShaderFactory.Create("Shaders/textureShader.vert", "Shaders/textureShader.frag");
        var quadVertexArray = VertexArrayFactory.Create();
        var whiteTexture = TextureFactory.Create("assets/whiteTexture.png").GetAwaiter().GetResult();

        _data = new Renderer2DStorage(quadVertexArray, textureShader, whiteTexture);

        float[] squareVertices =
        {
            // Position         Texture coordinates
            0.5f, 0.5f, 0.0f, 1.0f, 1.0f, // top right
            0.5f, -0.5f, 0.0f, 1.0f, 0.0f, // bottom right
            -0.5f, -0.5f, 0.0f, 0.0f, 0.0f, // bottom left
            -0.5f, 0.5f, 0.0f, 0.0f, 1.0f // top left
        };

        var squareVertexBuffer = VertexBufferFactory.Create(squareVertices);

        var layout = new BufferLayout(new[]
        {
            new BufferElement(ShaderDataType.Float3, "a_Position"),
            new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
        });

        squareVertexBuffer.SetLayout(layout);
        _data.QuadVertexArray.AddVertexBuffer(squareVertexBuffer);

        // var squareIndices = new uint[]
        // {
        //     0, 1, 2,
        //     2, 3, 0
        // };

        var squareIndices = new uint[]
        {
            0, 1, 3,
            1, 2, 3
        };

        var indexBuffer = IndexBufferFactory.Create(squareIndices, squareIndices.Length);
        _data.QuadVertexArray.SetIndexBuffer(indexBuffer);

        _data.TextureShader.Bind();
        _data.TextureShader.SetInt("u_Texture", 0);
    }

    public void Shutdown()
    {
    }

    public void BeginScene(OrthographicCamera camera)
    {
        _data.TextureShader.Bind();
        _data.TextureShader.SetMat4("u_ViewProjection", camera.ViewProjectionMatrix);
    }

    public void EndScene()
    {
    }

    public void DrawQuad(Vector2 position, Vector2 size, Vector4 color)
    {
        DrawQuad(new Vector3(position.X, position.Y, 0.0f), size, color);
    }

    public void DrawQuad(Vector3 position, Vector2 size, Vector4 color)
    {
        _data.TextureShader.SetFloat4("u_Color", color);

        // Tiling factor doesn't apply to flat color shader
        _data.TextureShader.SetFloat("u_TilingFactor", 1.0f);
        _data.WhiteTexture.Bind();

        var positionTranslated = Matrix4.CreateTranslation(position.X, position.Y, 0);
        var scale = Matrix4.CreateScale(size.X, size.Y, 1.0f);
        var transform = Matrix4.Identity * positionTranslated * scale; /* *rotation */
        _data.TextureShader.SetMat4("u_Transform", transform);

        _data.QuadVertexArray.Bind();
        RendererCommand.DrawIndexed(_data.QuadVertexArray);
    }

    public void DrawQuad(Vector2 position, Vector2 size, Texture2D texture, float tilingFactor = 1.0f, Vector4? tintColor = null)
    {
        DrawQuad(new Vector3(position.X, position.Y, 0.0f), size, texture, tilingFactor, tintColor);
    }

    //todo
    public void DrawQuad(Vector3 position, Vector2 size, Texture2D texture, float tilingFactor = 1.0f, Vector4? tintColor = null)
    {
        tintColor ??= Vector4.One;
        
        _data.TextureShader.SetFloat4("u_Color", tintColor.Value);
        _data.TextureShader.SetFloat("u_TilingFactor", tilingFactor);
        texture.Bind();

        var positionTranslated = Matrix4.CreateTranslation(position);
        var scale = Matrix4.CreateScale(size.X, size.Y, 1.0f);
        var transform = Matrix4.Identity * positionTranslated * scale; /* *rotation */
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
        
        var transform = Matrix4.CreateTranslation(position) *
                        Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotation)) *
                        Matrix4.CreateScale(size.X, size.Y, 1.0f);
        
        _data.TextureShader.SetMat4("u_Transform", transform);

        _data.QuadVertexArray.Bind();
        RendererCommand.DrawIndexed(_data.QuadVertexArray);
    }

    public void DrawRotatedQuad(Vector2 position, Vector2 size, float rotation, Texture2D texture, float tilingFactor, Vector4? tintColor = null)
    {
        DrawRotatedQuad(new Vector3(position.X, position.Y, 0.0f), size, rotation, texture, tilingFactor, tintColor);
    }

    public void DrawRotatedQuad(Vector3 position, Vector2 size, float rotation, Texture2D texture, float tilingFactor, Vector4? tintColor = null)
    {
        tintColor ??= Vector4.One;
        
        _data.TextureShader.SetFloat4("u_Color", tintColor.Value);
        _data.TextureShader.SetFloat("u_TilingFactor", tilingFactor);
        texture.Bind();
        
        var transform = Matrix4.CreateTranslation(position) *
                        Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotation)) *
                        Matrix4.CreateScale(size.X, size.Y, 1.0f);

        _data.TextureShader.SetMat4("u_Transform", transform);

        _data.QuadVertexArray.Bind();
        RendererCommand.DrawIndexed(_data.QuadVertexArray);
    }
}