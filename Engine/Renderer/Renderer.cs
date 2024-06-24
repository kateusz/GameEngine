using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Renderer.Shaders;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

public class Renderer
{
    public static readonly dynamic SceneData;
    
    private static Renderer? _instance;

    public static Renderer Instance => _instance ??= new Renderer();

    public void Init()
    {
        RendererCommand.Init();
        Renderer2D.Instance.Init();
    }
    
    public void BeginScene(OrthographicCamera camera)
    {
        SceneData.ViewProjectionMatrix = camera.ViewProjectionMatrix;
    }
    
    public void Submit(IShader shader, IVertexArray vertexArray, Matrix4x4 transform)
    {
        shader.Bind();
        shader.SetMat4("u_ViewProjection", SceneData.ViewProjectionMatrix);
        shader.SetMat4("u_Transform", transform);
        
        vertexArray.Bind();
        RendererCommand.DrawIndexed(vertexArray);
    }
    
    public void EndScene()
    {
        
    }
}