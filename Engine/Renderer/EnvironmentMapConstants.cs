namespace Engine.Renderer;

/// <summary>
/// Single source of truth for IBL map sizes. The values flow into the IBL
/// shaders via injected #defines (see OpenGLShader) so C# and GLSL cannot drift.
/// </summary>
internal static class EnvironmentMapConstants
{
    public const int EnvironmentMapSize = 512;
    public const int IrradianceSize = 32;
    public const int PrefilterSize = 128;
    public const int PrefilterMips = 5;
    public const float MaxReflectionLod = PrefilterMips - 1f;
    public const int BrdfLutSize = 512;
}