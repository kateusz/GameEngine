using System.Numerics;
using Engine.Core;
using Engine.Scene.Cameras;
using Engine.Scene.Skeletal;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;

namespace Engine.Renderer;

internal sealed class Graphics3D(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory,
    ITextureFactory textureFactory) : IGraphics3D
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.ForContext<Graphics3D>();
    private static readonly HashSet<int> LoggedSkinnedDrawEntities = [];
    private static readonly HashSet<int> LoggedLiveSkinnedDrawEntities = [];

    private const string ViewProjectionUniform = "u_ViewProjection";
    private const string BoneMatricesUniform = "u_BoneMatrices[0]";
    private static readonly Matrix4x4[] IdentityBonePalette = SkeletalPoseMath.CreateIdentityBonePalette();
    private const int BoneMatrixCount = SkeletalPoseMath.MaxBones;

    private IShader _cubeShader = null!;
    private IShader _texturedShader = null!;
    private IShader? _wireframeShader;
    private Mesh _cubeMesh = null!;

    private Vector3 _ambientColor = Vector3.One;
    private float _ambientStrength = 0.1f;
    private Vector3 _lightDirection = new(0, -1, 0);
    private Vector3 _lightColor = Vector3.Zero;

    private Matrix4x4 _viewProjection = Matrix4x4.Identity;
    private bool _wireframe;
    private bool _wireframeLoadFailed;
    private readonly Statistics _stats = new();
    private bool _disposed;
    private Matrix4x4[]? _lastUploadedBonePalette;
    private bool _identityBonesUploaded;

    public void Init()
    {
        _cubeShader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/flatColorShader.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/flatColorShader.frag"));
        _texturedShader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/lightingShader.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/lightingShader.frag"));
        _cubeMesh = meshFactory.CreateCube();

        _texturedShader.Bind();
        _texturedShader.SetInt("u_AlbedoMap", 0);
        _texturedShader.SetInt("u_MetallicRoughnessMap", 1);
        _texturedShader.SetInt("u_NormalMap", 2);
        // One-time identity palette so unweighted draws can skip SetMat4Array.
        _texturedShader.SetMat4Array(BoneMatricesUniform, IdentityBonePalette, (uint)BoneMatrixCount);
        _identityBonesUploaded = true;
        _lastUploadedBonePalette = IdentityBonePalette;
        _texturedShader.Unbind();

        SkinnedRenderDiagnostics.Once("graphics3d-lighting-shader", () =>
            _texturedShader.LogUniformInventory("lightingShader"));
    }

    public void BeginScene(Camera camera, Matrix4x4 transform)
    {
        if (!Matrix4x4.Invert(transform, out var viewMatrix))
        {
            Serilog.Log.ForContext<Graphics3D>().Error(
                "Failed to invert camera transform matrix (M11={M11}, M22={M22}, M33={M33}, M44={M44}). Skipping scene.",
                transform.M11, transform.M22, transform.M33, transform.M44);
            return;
        }

        ApplyCamera(viewMatrix * camera.GetProjectionMatrix(), new Vector3(transform.M41, transform.M42, transform.M43));
        SkinnedRenderDiagnostics.Once("camera-entity", () =>
            Logger.Debug("SkinnedDbg BeginScene Camera entity transform row4=({X:F3},{Y:F3},{Z:F3})",
                transform.M41, transform.M42, transform.M43));
    }

    public void BeginScene(IViewCamera camera)
    {
        var vp = camera.GetViewProjectionMatrix();
        var pos = camera.GetPosition();
        ApplyCamera(vp, pos);
        SkinnedRenderDiagnostics.Once("camera-viewcamera", () =>
        {
            Logger.Debug("SkinnedDbg BeginScene IViewCamera pos=({X:F3},{Y:F3},{Z:F3})", pos.X, pos.Y, pos.Z);
            SkinnedRenderDiagnostics.LogMatrix("u_ViewProjection", vp);
        });
    }

    public void EndScene()
    {
    }

    public void SetWireframe(bool enabled)
    {
        if (enabled)
        {
            if (EnsureWireframeShader())
                _wireframe = true;
            return;
        }

        _wireframe = false;
        rendererApi.SetPolygonMode(PolygonMode.Fill);
    }

    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1)
    {
        rendererApi.SetDepthTest(true);

        if (_wireframe)
        {
            DrawWireframe(_cubeMesh, transform, entityId);
            return;
        }

        BindCommon(_cubeShader, transform, color, entityId);

        _cubeMesh.Bind();
        rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
        _stats.DrawCalls++;
        _cubeShader.Unbind();
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, MeshMaterial material, Vector4 tint, float metallic, float roughness, int entityId = -1, Matrix4x4[]? boneMatrices = null)
    {
        rendererApi.SetDepthTest(true);

        if (_wireframe)
        {
            if (LoggedSkinnedDrawEntities.Add(entityId))
                Logger.Warning("SkinnedDbg wireframe draw — skinning skipped for entity={EntityId} mesh={Mesh}", entityId, mesh.Name);
            DrawWireframe(mesh, transform, entityId);
            return;
        }

        BindCommon(_texturedShader, transform, tint, entityId);
        var usingIdentityBones = boneMatrices is null or { Length: < BoneMatrixCount }
            || IsIdentityPalette(boneMatrices);
        var bones = usingIdentityBones ? IdentityBonePalette : boneMatrices!;
        // Live palettes are mutated in place each frame (same array ref) — must re-upload.
        // Identity: seed once in Init and skip until a live palette was uploaded.
        if (!usingIdentityBones)
        {
            _texturedShader.SetMat4Array(BoneMatricesUniform, bones, (uint)BoneMatrixCount);
            _lastUploadedBonePalette = bones;
            _identityBonesUploaded = false;
        }
        else if (!_identityBonesUploaded || !ReferenceEquals(_lastUploadedBonePalette, IdentityBonePalette))
        {
            _texturedShader.SetMat4Array(BoneMatricesUniform, IdentityBonePalette, (uint)BoneMatrixCount);
            _lastUploadedBonePalette = IdentityBonePalette;
            _identityBonesUploaded = true;
        }
        // ponytail: large mat4[] clobbers earlier uniforms on some GL drivers
        _texturedShader.SetMat4(ViewProjectionUniform, _viewProjection);
        _texturedShader.SetMat4("u_Model", transform);
        _texturedShader.SetMat4("u_NormalMatrix", ComputeNormalMatrix(transform));

        if (usingIdentityBones)
        {
            if (LoggedSkinnedDrawEntities.Add(entityId))
                LogSkinnedDrawDiagnostics(entityId, mesh, bones, usingIdentityBones: true);
        }
        else if (LoggedLiveSkinnedDrawEntities.Add(entityId))
        {
            LogSkinnedDrawDiagnostics(entityId, mesh, bones, usingIdentityBones: false);
            LogCpuSkinSample(entityId, mesh, bones);
        }

        _texturedShader.SetFloat("u_Metallic", metallic);
        _texturedShader.SetFloat("u_Roughness", roughness);
        _texturedShader.SetInt("u_HasAlbedoMap", material.HasAlbedoMap ? 1 : 0);
        _texturedShader.SetInt("u_HasMetallicRoughnessMap", material.HasMetallicRoughnessMap ? 1 : 0);
        _texturedShader.SetInt("u_HasNormalMap", material.HasNormalMap ? 1 : 0);

        (material.AlbedoTexture ?? textureFactory.GetWhiteTexture()).Bind(0);
        (material.MetallicRoughnessTexture ?? textureFactory.GetWhiteTexture()).Bind(1);
        (material.NormalTexture ?? textureFactory.GetFlatNormalTexture()).Bind(2);

        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
        _texturedShader.Unbind();
    }

    private static void LogSkinnedDrawDiagnostics(int entityId, Mesh mesh, Matrix4x4[] bones, bool usingIdentityBones)
    {
        var hasWeights = mesh.Vertices.Any(v =>
            v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W > 1e-5f);
        if (!hasWeights && usingIdentityBones)
            return;

        Logger.Debug(
            "SkinnedDbg DrawMesh entity={EntityId} mesh={Mesh} indices={IndexCount} identityBones={IdentityBones}",
            entityId, mesh.Name, mesh.GetIndexCount(), usingIdentityBones);
        SkinnedRenderDiagnostics.LogBonePalette($"gpu-entity-{entityId}", bones);
    }
    
    private static void LogCpuSkinSample(int entityId, Mesh mesh, Matrix4x4[] bones)
    {
        var maxDisp = 0f;
        var samples = 0;
        var step = System.Math.Max(1, mesh.Vertices.Count / 256);
        for (var i = 0; i < mesh.Vertices.Count && samples < 256; i += step)
        {
            var v = mesh.Vertices[i];
            var w = v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W;
            if (w < 1e-5f)
                continue;

            samples++;
            var skinned = SkinPositionCpu(v, bones);
            maxDisp = MathF.Max(maxDisp, Vector3.Distance(v.Position, skinned));
        }

        Logger.Debug(
            "SkinnedDbg cpuSkin entity={EntityId} samples={Samples} maxDisp={MaxDisp:F3} (if bounded but viewport explodes → GPU attrib/uniform)",
            entityId, samples, maxDisp);
    }

    private static Vector3 SkinPositionCpu(Mesh.Vertex v, Matrix4x4[] palette)
    {
        var weightSum = v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W;
        if (weightSum < 1e-5f)
            return v.Position;

        var pos = Vector4.Zero;
        SkinAccumulate(ref pos, v.Position, v.BoneIndex.X, v.BoneWeight.X, palette);
        SkinAccumulate(ref pos, v.Position, v.BoneIndex.Y, v.BoneWeight.Y, palette);
        SkinAccumulate(ref pos, v.Position, v.BoneIndex.Z, v.BoneWeight.Z, palette);
        SkinAccumulate(ref pos, v.Position, v.BoneIndex.W, v.BoneWeight.W, palette);
        return new Vector3(pos.X, pos.Y, pos.Z);
    }

    private static void SkinAccumulate(ref Vector4 pos, Vector3 p, float boneIndexF, float weight, Matrix4x4[] palette)
    {
        if (weight <= 0f)
            return;
        var idx = (int)(boneIndexF + 0.5f);
        // Match GLSL BoneIndices() clamp to last bone (MaxBones-1), not force bone 0.
        var maxIdx = System.Math.Min(palette.Length, BoneMatrixCount) - 1;
        if (maxIdx < 0)
            return;
        if (idx < 0)
            idx = 0;
        else if (idx > maxIdx)
            idx = maxIdx;
        pos += Vector4.Transform(new Vector4(p, 1f), palette[idx]) * weight;
    }

    private static bool IsIdentityPalette(Matrix4x4[] bones)
    {
        for (var i = 0; i < bones.Length; i++)
        {
            if (bones[i] != Matrix4x4.Identity)
                return false;
        }
        return true;
    }

    public void SetAmbientLight(Vector3 color, float strength)
    {
        _ambientColor = color;
        _ambientStrength = strength;
    }

    public void SetDirectionalLight(Vector3 direction, Vector3 color)
    {
        _lightDirection = direction;
        _lightColor = color;
    }

    private void ApplyCamera(Matrix4x4 viewProjection, Vector3 viewPosition)
    {
        _viewProjection = viewProjection;

        _cubeShader.Bind();
        _cubeShader.SetMat4(ViewProjectionUniform, viewProjection);

        _texturedShader.Bind();
        _texturedShader.SetMat4(ViewProjectionUniform, viewProjection);
        _texturedShader.SetFloat3("u_ViewPosition", viewPosition);

        if (_wireframeShader is not null)
        {
            _wireframeShader.Bind();
            _wireframeShader.SetMat4(ViewProjectionUniform, viewProjection);
        }
    }

    private void DrawWireframe(Mesh mesh, Matrix4x4 transform, int entityId)
    {
        _wireframeShader!.Bind();
        _wireframeShader.SetMat4(ViewProjectionUniform, _viewProjection);
        _wireframeShader.SetMat4("u_Model", transform);
        _wireframeShader.SetFloat4("u_Color", RenderingConstants.WireframeEdgeColor);
        _wireframeShader.SetInt("u_EntityID", entityId);
        mesh.Bind();
        try
        {
            rendererApi.SetPolygonMode(PolygonMode.Line);
            rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
            _stats.DrawCalls++;
        }
        finally
        {
            rendererApi.SetPolygonMode(PolygonMode.Fill);
            _wireframeShader.Unbind();
        }
    }

    private bool EnsureWireframeShader()
    {
        if (_wireframeShader is not null)
            return true;
        if (_wireframeLoadFailed)
            return false;

        try
        {
            // ponytail: host BaseDirectory — project AssetsPath has no Editor shaders; latch until Dispose
            _wireframeShader = shaderFactory.Create(
                ResolveHostShader("wireframeShader.vert"),
                ResolveHostShader("wireframeShader.frag"));
            _wireframeShader.Bind();
            _wireframeShader.SetMat4(ViewProjectionUniform, _viewProjection);
            _wireframeShader.Unbind();
            return true;
        }
        catch (Exception ex)
        {
            Serilog.Log.ForContext<Graphics3D>().Error(ex, "Failed to load wireframe shader; falling back to Normal");
            _wireframeShader = null;
            _wireframe = false;
            _wireframeLoadFailed = true;
            rendererApi.SetPolygonMode(PolygonMode.Fill);
            return false;
        }
    }

    private static string ResolveHostShader(string fileName) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL", fileName));

    private void BindCommon(IShader shader, Matrix4x4 transform, Vector4 color, int entityId)
    {
        shader.Bind();
        shader.SetMat4("u_Model", transform);
        shader.SetMat4("u_NormalMatrix", ComputeNormalMatrix(transform));
        shader.SetFloat4("u_Color", color);
        shader.SetInt("u_EntityID", entityId);
        shader.SetFloat3("lightColor", _ambientColor);
        shader.SetFloat("strength", _ambientStrength);
        shader.SetFloat3("u_LightDirection", _lightDirection);
        shader.SetFloat3("u_LightColor", _lightColor);
    }

    private static Matrix4x4 ComputeNormalMatrix(Matrix4x4 model) =>
        Matrix4x4.Invert(model, out var inv) ? Matrix4x4.Transpose(inv) : Matrix4x4.Identity;

    public void ResetStats()
    {
        _stats.DrawCalls = 0;
    }

    public Statistics GetStats() => _stats;

    public void SetClearColor(Vector4 color) => rendererApi.SetClearColor(color);

    public void Clear() => rendererApi.Clear();

    public void Dispose()
    {
        if (_disposed)
            return;

        _cubeShader?.Dispose();
        _cubeShader = null!;
        _texturedShader?.Dispose();
        _texturedShader = null!;
        _wireframeShader?.Dispose();
        _wireframeShader = null;
        _cubeMesh?.Dispose();
        _cubeMesh = null!;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
