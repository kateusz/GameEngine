using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.VertexArray;

namespace Engine.Renderer;

public record Renderer2DStorage(IVertexArray QuadVertexArray, IShader TextureShader, Texture2D WhiteTexture);