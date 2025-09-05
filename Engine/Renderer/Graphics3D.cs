using System.Numerics;
using Engine.Platform;
using Engine.Renderer.Cameras;
using Engine.Renderer.Shaders;
using Engine.Scene.Components;

namespace Engine.Renderer;

public class Graphics3D : IGraphics3D
{
    private static IGraphics3D? _instance;
    public static IGraphics3D Instance => _instance ??= new Graphics3D();
    
    private IRendererAPI _rendererApi = RendererApiFactory.Create();
    private IShader _phongShader;
    private Vector3 _lightPosition = new Vector3(0.0f, 3.0f, 3.0f);
    private Vector3 _lightColor = new Vector3(1.0f, 1.0f, 1.0f);
    private float _shininess = 32.0f;
    
    private Statistics _stats = new();

    public void Init()
    {
        _phongShader = ShaderFactory.Create("assets/shaders/OpenGL/phong.vert", "assets/shaders/OpenGL/phong.frag");
    }

    public void BeginScene(Camera camera, Matrix4x4 transform)
    {
        _ = Matrix4x4.Invert(transform, out var transformInverted);
        Matrix4x4? viewProj = null;

        if (OSInfo.IsWindows)
        {
            viewProj = camera.Projection * transformInverted;
        }
        else if (OSInfo.IsMacOS)
        {
            viewProj = transformInverted * camera.Projection;
        }
        else
            throw new InvalidOperationException("Unsupported OS version!");
        
        _phongShader.Bind();
        _phongShader.SetMat4("u_ViewProjection", viewProj.Value);
        _phongShader.SetFloat3("u_LightPosition", _lightPosition);
        _phongShader.SetFloat3("u_LightColor", _lightColor);
        _phongShader.SetFloat3("u_ViewPosition", new Vector3(transform.M41, transform.M42, transform.M43));
        _phongShader.SetFloat("u_Shininess", _shininess);
    }
    
    public void BeginScene(EditorCamera camera)
    {
        var viewProj = camera.GetViewProjection();
        
        _phongShader.Bind();
        _phongShader.SetMat4("u_ViewProjection", viewProj);
        _phongShader.SetFloat3("u_LightPosition", _lightPosition);
        _phongShader.SetFloat3("u_LightColor", _lightColor);
        _phongShader.SetFloat3("u_ViewPosition", camera.GetPosition());
        _phongShader.SetFloat("u_Shininess", _shininess);
    }

    public void EndScene()
    {
        _phongShader.Unbind();
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 color, int entityId = -1)
    {
        _phongShader.Bind();
        _phongShader.SetMat4("u_Model", transform);
        
        // Calculate normal matrix (inverse transpose of the model matrix)
        var normalMatrix = transform;
        bool inverted = Matrix4x4.Invert(normalMatrix, out normalMatrix);
        if (inverted)
        {
            normalMatrix = Matrix4x4.Transpose(normalMatrix);
        }
        _phongShader.SetMat4("u_NormalMatrix", normalMatrix);
        
        _phongShader.SetFloat4("u_Color", color);
        
        // Check if we're using a texture
        bool hasTexture = mesh.DiffuseTexture.Width > 1;
        _phongShader.SetInt("u_UseTexture", hasTexture ? 1 : 0);
        
        // Bind the mesh
        mesh.Initialize();
        mesh.Bind();
        
        // Draw
        _rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }
    
    public void DrawModel(Matrix4x4 transform, MeshComponent meshComponent, ModelRendererComponent modelRenderer, int entityId = -1)
    {
        var mesh = meshComponent.Mesh;
        var color = modelRenderer.Color;
        
        // If an override texture is specified, temporarily replace the mesh's texture
        var originalTexture = mesh.DiffuseTexture;
        if (modelRenderer.OverrideTexture != null)
        {
            mesh.DiffuseTexture = modelRenderer.OverrideTexture;
        }
        
        DrawMesh(transform, mesh, color, entityId);
        
        // Restore the original texture
        if (modelRenderer.OverrideTexture != null)
        {
            mesh.DiffuseTexture = originalTexture;
        }
    }
    
    // Set light properties
    public void SetLightPosition(Vector3 position)
    {
        _lightPosition = position;
    }
    
    public void SetLightColor(Vector3 color)
    {
        _lightColor = color;
    }
    
    public void SetShininess(float shininess)
    {
        _shininess = shininess;
    }
    
    // Stats
    public void ResetStats()
    {
        _stats.DrawCalls = 0;
    }
    
    public Statistics GetStats()
    {
        return _stats;
    }
    
    public void SetClearColor(Vector4 color) => _rendererApi.SetClearColor(color);

    public void Clear() => _rendererApi.Clear();
}