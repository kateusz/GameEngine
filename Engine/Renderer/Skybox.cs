using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Shaders;
using Engine.Renderer.VertexArray;
using Silk.NET.OpenGL;
using Engine.Platform.SilkNet;
using Engine.Renderer.Cameras;
using StbImageSharp;

namespace Engine.Renderer;

public class Skybox : IBindable, IDisposable
{
    private uint _cubemapTextureId;
    private IShader _skyboxShader;
    private IVertexArray _skyboxVao;
    private IVertexBuffer _skyboxVbo;
    private IIndexBuffer _skyboxIbo;
    
    // Vertices for a cube centered at the origin with size 1
    private static readonly float[] SkyboxVertices = {
        // Positions          
        -1.0f,  1.0f, -1.0f,
        -1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f,  1.0f,
         1.0f,  1.0f,  1.0f
    };

    // Indices for the skybox cube
    private static readonly uint[] SkyboxIndices = {
        // Right
        3, 2, 6, 6, 7, 3,
        // Left
        4, 5, 1, 1, 0, 4,
        // Top
        4, 0, 3, 3, 7, 4,
        // Bottom
        1, 5, 6, 6, 2, 1,
        // Back
        0, 1, 2, 2, 3, 0,
        // Front
        7, 6, 5, 5, 4, 7
    };

    /// <summary>
    /// Creates a new skybox with the specified face textures
    /// </summary>
    /// <param name="facePaths">Array of 6 paths to the skybox textures in the order:
    /// Right (+X), Left (-X), Top (+Y), Bottom (-Y), Front (+Z), Back (-Z)</param>
    public Skybox(string[] facePaths)
    {
        if (facePaths.Length != 6)
            throw new ArgumentException("Skybox requires exactly 6 face textures", nameof(facePaths));
        
        LoadCubemap(facePaths);
        InitSkyboxMesh();
        _skyboxShader = ShaderFactory.Create("assets/shaders/OpenGL/skybox.vert", "assets/shaders/OpenGL/skybox.frag");
    }

    /// <summary>
    /// Loads the 6 face textures as a cubemap texture
    /// </summary>
    private void LoadCubemap(string[] facePaths)
    {
        _cubemapTextureId = SilkNetContext.GL.GenTexture();
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, _cubemapTextureId);

        StbImage.stbi_set_flip_vertically_on_load(0); // Don't flip cubemap textures vertically

        for (int i = 0; i < facePaths.Length; i++)
        {
            using (var stream = File.OpenRead(facePaths[i]))
            {
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                
                unsafe
                {
                    fixed (byte* ptr = image.Data)
                    {
                        SilkNetContext.GL.TexImage2D(
                            TextureTarget.TextureCubeMapPositiveX + i, 
                            0, 
                            InternalFormat.Rgba8, 
                            (uint)image.Width, 
                            (uint)image.Height, 
                            0, 
                            PixelFormat.Rgba, 
                            PixelType.UnsignedByte, 
                            ptr
                        );
                    }
                }
            }
        }

        // Set texture parameters
        SilkNetContext.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        SilkNetContext.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        SilkNetContext.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        SilkNetContext.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        SilkNetContext.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
    }

    /// <summary>
    /// Initializes the cube mesh for the skybox
    /// </summary>
    private void InitSkyboxMesh()
    {
        _skyboxVao = VertexArrayFactory.Create();
        _skyboxVao.Bind();
        
        // Create and bind the vertex buffer
        _skyboxVbo = VertexBufferFactory.Create((uint)(SkyboxVertices.Length * sizeof(float)));
        _skyboxVbo.Bind();
        
        unsafe
        {
            fixed (float* vertexPtr = SkyboxVertices)
            {
                SilkNetContext.GL.BufferData(
                    BufferTargetARB.ArrayBuffer, 
                    (nuint)(SkyboxVertices.Length * sizeof(float)), 
                    vertexPtr, 
                    BufferUsageARB.StaticDraw
                );
            }
        }
        
        // Set up the vertex attributes
        var layout = new BufferLayout(new[] {
            new BufferElement(ShaderDataType.Float3, "a_Position")
        });
        _skyboxVbo.SetLayout(layout);
        _skyboxVao.AddVertexBuffer(_skyboxVbo);
        
        // Create and bind the index buffer
        _skyboxIbo = IndexBufferFactory.Create(SkyboxIndices, SkyboxIndices.Length);
        _skyboxVao.SetIndexBuffer(_skyboxIbo);
    }

    /// <summary>
    /// Renders the skybox
    /// </summary>
    /// <param name="camera">The camera to use for rendering</param>
    public void Render(Camera camera)
    {
        // Save the current depth function
        SilkNetContext.GL.GetInteger(GetPName.DepthFunc, out int originalDepthFunc);
        
        // Change depth function so depth test passes when values are equal to depth buffer's content
        SilkNetContext.GL.DepthFunc(DepthFunction.Lequal);
        
        _skyboxShader.Bind();
        
        // Remove translation from the view matrix to keep skybox centered on camera
        var viewMatrix = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(0, 0, 0));
        
        _skyboxShader.SetMat4("u_View", viewMatrix);
        _skyboxShader.SetMat4("u_Projection", camera.Projection);
        
        // Bind the cubemap texture
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, _cubemapTextureId);
        
        _skyboxShader.SetInt("u_Skybox", 0);
        
        // Render the cube
        _skyboxVao.Bind();
        RendererCommand.DrawIndexed(_skyboxVao);
        
        // Reset depth function
        SilkNetContext.GL.DepthFunc((DepthFunction)originalDepthFunc);
    }

    public void Render(EditorCamera camera)
    {
        // Save the current depth function
        SilkNetContext.GL.GetInteger(GetPName.DepthFunc, out int originalDepthFunc);
        
        // Change depth function so depth test passes when values are equal to depth buffer's content
        SilkNetContext.GL.DepthFunc(DepthFunction.Lequal);
        
        _skyboxShader.Bind();
        
        // Extract view matrix without translation
        var viewMatrix = camera.GetViewMatrix();
        viewMatrix.M41 = 0;
        viewMatrix.M42 = 0;
        viewMatrix.M43 = 0;
        
        _skyboxShader.SetMat4("u_View", viewMatrix);
        _skyboxShader.SetMat4("u_Projection", camera.Projection);
        
        // Bind the cubemap texture
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0);
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, _cubemapTextureId);
        
        _skyboxShader.SetInt("u_Skybox", 0);
        
        // Render the cube
        _skyboxVao.Bind();
        RendererCommand.DrawIndexed(_skyboxVao);
        
        // Reset depth function
        SilkNetContext.GL.DepthFunc((DepthFunction)originalDepthFunc);
    }

    public void Bind()
    {
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, _cubemapTextureId);
    }

    public void Unbind()
    {
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, 0);
    }

    public void Dispose()
    {
        SilkNetContext.GL.DeleteTexture(_cubemapTextureId);
    }
}