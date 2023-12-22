using Engine.Renderer.Shaders;

namespace Engine.Renderer;

public record Renderer2DStorage(IVertexArray QuadVertexArray, IShader TextureShader, Texture2D WhiteTexture);