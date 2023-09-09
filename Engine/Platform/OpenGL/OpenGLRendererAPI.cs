using Engine.Renderer;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Platform.OpenGL;

public class SceneData
{
    public Matrix4 ViewProjectionMatrix { get; set; }
}

public class OpenGLRendererAPI : IRendererAPI
{
    public static readonly SceneData SceneData = new();

    public ApiType ApiType { get; } = ApiType.OpenGL;

    public static void BeginScene(OrthographicCamera camera)
    {
        SceneData.ViewProjectionMatrix = camera.ViewProjectionMatrix;
    }

    public static void EndScene()
    {
    }

    public static void Submit(IShader shader, IVertexArray vertexArray, Matrix4 transform)
    {
        shader.Bind();
        shader.UploadUniformMatrix4("u_ViewProjection", SceneData.ViewProjectionMatrix);
        shader.UploadUniformMatrix4("u_Transform", transform);
        
        vertexArray.Bind();
        RendererCommand.DrawIndexed(vertexArray);
    }

    public void SetClearColor(Vector4 color)
    {
        GL.ClearColor(color.X, color.Y, color.Z, color.W);
    }

    public void Clear()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void DrawIndexed(IVertexArray vertexArray)
    {
        var indexBuffer = vertexArray.IndexBuffer;
        GL.DrawElements(PrimitiveType.Triangles, indexBuffer.GetCount(), DrawElementsType.UnsignedInt, 0);
    }
}