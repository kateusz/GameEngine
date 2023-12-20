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
        //var textureShader = ShaderFactory.Create("Shaders/textureShader.vert", "Shaders/textureShader.frag");
        var flatColorShader = ShaderFactory.Create("Shaders/flatColorShader.vert", "Shaders/flatColorShader.frag");
        var quadVertexArray = VertexArrayFactory.Create();
        
        _data = new Renderer2DStorage(quadVertexArray, flatColorShader);

        // float[] squareVertices =
        // {
        //     -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
        //     0.5f, -0.5f, 0.0f, 1.0f, 0.0f,
        //     0.5f, 0.5f, 0.0f, 1.0f, 1.0f
        //     -0.5f, 0.5f, 0.0f, 0.0f, 1.0f, 
        // };
        
        float[] squareVertices =
        {
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            0.5f, 0.5f, 0.0f,
            -0.5f, 0.5f, 0.0f,
        };

        var squareVertexBuffer = VertexBufferFactory.Create(squareVertices);

        var layout = new BufferLayout(new[]
        {
            new BufferElement(ShaderDataType.Float3, "a_Position"),
           // new BufferElement(ShaderDataType.Float2, "a_TexCoord"),
        });

        squareVertexBuffer.SetLayout(layout);
        _data.QuadVertexArray.AddVertexBuffer(squareVertexBuffer);

        var squareIndices = new uint[]
        {
            0, 1, 2,
            2, 3, 0
        };

        var indexBuffer = IndexBufferFactory.Create(squareIndices, squareIndices.Length);
        _data.QuadVertexArray.SetIndexBuffer(indexBuffer);
        
        //_data.Shader.Bind();
        //_data.TextureShader.SetInt("u_Texture", 0);
    }

    public void Shutdown()
    {
        
    }

    public void BeginScene(OrthographicCamera camera)
    {
        _data.Shader.Bind();
        _data.Shader.SetMat4("u_ViewProjection", camera.ViewProjectionMatrix);
        _data.Shader.SetMat4("u_Transform", Matrix4.Identity);
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
        _data.Shader.Bind();
        _data.Shader.SetFloat4("u_Color", color);
        // bind white texture here
        
        var positionTranslated = Matrix4.CreateTranslation(position.X, position.Y, 0);
        var scale = Matrix4.CreateScale(size.X, size.Y, 1.0f);
        var transform = Matrix4.Identity * positionTranslated * scale; /* *rotation */
        _data.Shader.SetMat4("u_Transform", transform);
        
        _data.QuadVertexArray.Bind();
        RendererCommand.DrawIndexed(_data.QuadVertexArray);
    }
    
    public void DrawQuad(Vector2 position, Vector2 size, Texture2D texture)
    {
        DrawQuad(new Vector3(position.X, position.Y, 0.0f), size, texture);
    }
    
    //todo
    public void DrawQuad(Vector3 position, Vector2 size, Texture2D texture)
    {
        _data.Shader.SetFloat4("u_Color", Vector4.One);
        texture.Bind();
        
        var positionTranslated = Matrix4.CreateTranslation(position.X, position.Y, 0);
        var scale = Matrix4.CreateScale(size.X, size.Y, 1.0f);
        var transform = Matrix4.Identity * positionTranslated * scale; /* *rotation */
        _data.Shader.SetMat4("u_Transform", transform);
        
        _data.QuadVertexArray.Bind();
        RendererCommand.DrawIndexed(_data.QuadVertexArray);
    }
}