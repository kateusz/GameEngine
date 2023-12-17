using OpenTK.Mathematics;

namespace Engine.Renderer;

public class Renderer2D
{
    private static Renderer2D _instance;

    public static Renderer2D Instance => _instance ??= new Renderer2D();
    
    private IVertexBuffer _squareVertexBuffer;
    private IIndexBuffer _indexBuffer;
    private IVertexArray _quadVertexArray;
    private IShader _flatColorShader;
    private uint[] _squareIndices;
    private OrthographicCameraController _cameraController;
    private Vector3 _cameraPosition = Vector3.Zero;
    
    
    public void Init()
    {
        _quadVertexArray = VertexArrayFactory.Create();

        float[] squareVertices =
        {
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            0.5f, 0.5f, 0.0f,
            -0.5f, 0.5f, 0.0f,
        };

        _squareVertexBuffer = VertexBufferFactory.Create(squareVertices);

        var layout = new BufferLayout(new[]
        {
            new BufferElement(ShaderDataType.Float3, "a_Position")
        });

        _squareVertexBuffer.SetLayout(layout);
        _quadVertexArray.AddVertexBuffer(_squareVertexBuffer);

        _squareIndices = new uint[]
        {
            0, 1, 2,
            2, 3, 0
        };

        _indexBuffer = IndexBufferFactory.Create(_squareIndices, 6);
        _quadVertexArray.SetIndexBuffer(_indexBuffer);
        _flatColorShader = ShaderFactory.Create("Shaders/flatColorShader.vert", "Shaders/flatColorShader.frag");
        
        //_flatColorShader.Bind();
        //_flatColorShader.UploadUniformInt("u_Texture", 0);
    }

    public void Shutdown()
    {
        
    }

    public void BeginScene(OrthographicCamera camera)
    {
        _flatColorShader.Bind();
        _flatColorShader.UploadUniformMatrix4("u_ViewProjection", camera.ViewProjectionMatrix);
        _flatColorShader.UploadUniformMatrix4("u_Transform", Matrix4.Identity);
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
        _flatColorShader.Bind();
        _flatColorShader.UploadUniformFloat4("u_Color", color);
        
        _quadVertexArray.Bind();
        RendererCommand.DrawIndexed(_quadVertexArray);
        
    }
}