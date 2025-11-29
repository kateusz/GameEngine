namespace Engine.Renderer;

/// <summary>
/// Centralized constants for the 2D rendering system.
/// Defines all magic numbers used throughout the renderer to improve maintainability
/// and prevent inconsistencies.
/// </summary>
internal static class RenderingConstants
{
    // Batch configuration
    /// <summary>
    /// Maximum number of quads that can be batched in a single draw call.
    /// Industry standard for 2D batch renderers.
    /// </summary>
    public const int DefaultMaxQuads = 10000;
    
    /// <summary>
    /// Maximum number of texture slots available for batching.
    /// OpenGL minimum guaranteed texture units (GL_MAX_TEXTURE_IMAGE_UNITS).
    /// </summary>
    public const int MaxTextureSlots = 16;

    // Quad geometry
    /// <summary>
    /// Number of vertices per quad (4 corners).
    /// </summary>
    public const int QuadVertexCount = 4;
    
    /// <summary>
    /// Number of indices per quad (6 indices for 2 triangles).
    /// </summary>
    public const int QuadIndexCount = 6;

    // Texture defaults
    /// <summary>
    /// White color in RGBA format (0xFFFFFFFF).
    /// Used for the default white texture.
    /// </summary>
    public const uint WhiteTextureColor = 0xFFFFFFFF;
    
    /// <summary>
    /// Black color in RGBA format (0xFF000000).
    /// </summary>
    public const uint BlackTextureColor = 0xFF000000;

    // Performance tuning
    /// <summary>
    /// Default line width for line rendering in pixels.
    /// </summary>
    public const float DefaultLineWidth = 1.0f;
    
    /// <summary>
    /// Maximum framebuffer size in pixels.
    /// Common hardware limit for texture/framebuffer dimensions.
    /// </summary>
    public const uint MaxFramebufferSize = 8192;

    // Data type sizes (bytes)
    /// <summary>
    /// Size of a float in bytes (32-bit float).
    /// </summary>
    public const int FloatSize = 4;
    
    /// <summary>
    /// Size of an int in bytes (32-bit integer).
    /// </summary>
    public const int IntSize = 4;
    
    /// <summary>
    /// Size of a bool in bytes.
    /// </summary>
    public const int BoolSize = 1;
    
    // Computed constants
    /// <summary>
    /// Maximum number of vertices that can be batched (MaxQuads * QuadVertexCount).
    /// </summary>
    public const int MaxVertices = DefaultMaxQuads * QuadVertexCount;
    
    /// <summary>
    /// Maximum number of indices that can be batched (MaxQuads * QuadIndexCount).
    /// </summary>
    public const int MaxIndices = DefaultMaxQuads * QuadIndexCount;
}
