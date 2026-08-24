using Engine.Platform.OpenGL.Buffers;
using Engine.Renderer.Buffers;
using Engine.Renderer.Meshes;

namespace Engine.Platform.OpenGL;

internal sealed class VertexBufferFactory : IVertexBufferFactory
{
    public IVertexBuffer Create(uint size) => new OpenGLVertexBuffer(size);

    public IVertexBuffer Create(List<Mesh.Vertex> vertices) => new OpenGLVertexBuffer(vertices);
}
