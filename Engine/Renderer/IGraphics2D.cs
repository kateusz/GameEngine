using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Components;

namespace Engine.Renderer;

public interface IGraphics2D : IGraphics
{
    void Init();
    void Shutdown();

    /// <summary>
    /// Begins a scene with a legacy OrthographicCamera. This method is deprecated.
    /// </summary>
    /// <param name="camera">The legacy orthographic camera.</param>
    /// <remarks>
    /// <para><b>DEPRECATED:</b> This method uses the legacy camera system.</para>
    /// <para><b>Migration:</b> Use <see cref="BeginScene(Camera, Matrix4x4)"/> with SceneCamera instead.</para>
    /// </remarks>
    [Obsolete("Use BeginScene(Camera, Matrix4x4) with SceneCamera instead. This legacy camera system will be removed in a future version.")]
    void BeginScene(OrthographicCamera camera);

    void BeginScene(Camera camera, Matrix4x4 transform);
    void EndScene();
    void DrawQuad(Vector2 position, Vector2 size, Vector4 color);
    void DrawQuad(Vector3 position, Vector2 size, Vector4 color);
    void DrawQuad(Vector3 position, Vector2 size, float rotation, SubTexture2D subTexture);
    void DrawQuad(Vector2 position, Vector2 size, Texture2D texture, float tilingFactor = 1.0f, Vector4? tintColor = null);
    void DrawQuad(Vector3 position, Vector2 size, Texture2D? texture, Vector2[] textureCoords, float tilingFactor = 1.0f, Vector4? tintColor = null);
    void DrawQuad(Vector3 position, Vector2 size, float rotation, Texture2D? texture, Vector2[] textureCoords, float tilingFactor = 1.0f, Vector4? tintColor = null);
    void DrawQuad(Matrix4x4 transform, Vector4 color, int entityId = -1);

    /// <summary>
    /// Draws a textured or colored quad
    /// </summary>
    /// <param name="transform">
    /// The transformation matrix applied to the quad's vertices. This matrix defines the position, rotation, and scaling of the quad.
    /// </param>
    /// <param name="texture">
    /// The texture applied to the quad. If <c>null</c>, the quad will be drawn with a solid color using the specified <paramref name="tintColor"/>.
    /// </param>
    /// <param name="textureCoords">tbd</param>
    /// <param name="tilingFactor">
    /// The tiling factor for the texture coordinates. This value determines how many times the texture repeats over the quad. Default is 1.0f (no tiling).
    /// </param>
    /// <param name="tintColor">
    /// The color tint applied to the quad. If <c>null</c>, the default value is <see cref="Vector4.One"/>, representing white (no tint).
    /// </param>
    /// <param name="entityId">
    /// The ID of the entity associated with the quad. Used for identifying the quad in entity-based systems. Default is -1 (no entity).
    /// </param>
    /// <remarks>
    /// This method handles batching of quads and may trigger a new batch if the current batch reaches the maximum allowed indices or texture slots.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the maximum number of texture slots is exceeded when a new texture is added.
    /// </exception>
    void DrawQuad(Matrix4x4 transform, Texture2D? texture, Vector2[] textureCoords, float tilingFactor = 1.0f, Vector4? tintColor = null, int entityId = -1);

    void DrawSprite(Matrix4x4 transform, SpriteRendererComponent src, int entityId);
    void DrawLine(Vector3 p0, Vector3 p1, Vector4 color, int entityId);
    void DrawRect(Vector3 position, Vector2 size, Vector4 color, int entityId);
    void DrawRect(Matrix4x4 transform, Vector4 color, int entityId);
    void ResetStats();
    Statistics GetStats();
}