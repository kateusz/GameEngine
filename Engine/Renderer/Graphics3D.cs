using System.Numerics;
using Engine.Platform;
using Engine.Renderer.Cameras;
using Engine.Renderer.Shaders;
using Engine.Scene.Components;

namespace Engine.Renderer;

internal sealed class Graphics3D(IRendererAPI rendererApi, IShaderFactory shaderFactory) : IGraphics3D
{
    private IShader _phongShader = null!;
    private Vector3 _lightPosition = new(0.0f, 3.0f, 3.0f);
    private Vector3 _lightColor = new(1.0f, 1.0f, 1.0f);
    private float _shininess = 32.0f;

    private readonly Statistics _stats = new();
    private bool _disposed;

    public void Init()
    {
        _phongShader = shaderFactory.Create("assets/shaders/opengl/phong.vert", "assets/shaders/opengl/phong.frag");
    }

    public void BeginScene(Camera camera, Matrix4x4 transform)
    {
        _ = Matrix4x4.Invert(transform, out var transformInverted);
        Matrix4x4? viewProj = null;

        if (OSInfo.IsWindows)
        {
            viewProj = camera.GetProjectionMatrix() * transformInverted;
        }
        else if (OSInfo.IsMacOS)
        {
            viewProj = transformInverted * camera.GetProjectionMatrix();
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
        var inverted = Matrix4x4.Invert(normalMatrix, out normalMatrix);
        if (inverted)
        {
            normalMatrix = Matrix4x4.Transpose(normalMatrix);
        }
        _phongShader.SetMat4("u_NormalMatrix", normalMatrix);

        _phongShader.SetFloat4("u_Color", color);

        // Check if we're using a texture
        var hasTexture = mesh.DiffuseTexture.Width > 1;
        _phongShader.SetInt("u_UseTexture", hasTexture ? 1 : 0);

        // Bind the mesh (already initialized)
        mesh.Bind();

        // Draw
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    public void DrawModel(Matrix4x4 transform, MeshComponent meshComponent, ModelRendererComponent modelRenderer, int entityId = -1)
    {
        var mesh = meshComponent.Mesh;
        if (mesh == null)
            return;

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

    public void SetClearColor(Vector4 color) => rendererApi.SetClearColor(color);

    public void Clear() => rendererApi.Clear();

    public void Dispose()
    {
        if (_disposed)
            return;

        _phongShader?.Dispose();

        _disposed = true;
    }
}
