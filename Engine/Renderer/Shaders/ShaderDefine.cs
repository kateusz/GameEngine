namespace Engine.Renderer.Shaders;

/// <summary>A preprocessor define injected into a shader's source after the #version line.</summary>
public readonly record struct ShaderDefine(string Name, string Value);