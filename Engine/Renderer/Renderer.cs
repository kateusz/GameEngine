using Engine.Platform.OpenGL;
using OpenTK.Mathematics;

namespace Engine.Renderer;

public class Renderer
{
    public static readonly SceneData SceneData = new();
    
    private static Renderer _instance;

    public static Renderer Instance => _instance ??= new Renderer();

    public void Init()
    {
        RendererCommand.Init();
    }
    
    public void BeginScene(OrthographicCamera camera)
    {
        SceneData.ViewProjectionMatrix = camera.ViewProjectionMatrix;
    }
    
    public void Submit(IShader shader, IVertexArray vertexArray, Matrix4 transform)
    {
        shader.Bind();
        shader.UploadUniformMatrix4("u_ViewProjection", SceneData.ViewProjectionMatrix);
        shader.UploadUniformMatrix4("u_Transform", transform);
        
        vertexArray.Bind();
        RendererCommand.DrawIndexed(vertexArray);
    }
    
    public void EndScene()
    {
        
    }
}