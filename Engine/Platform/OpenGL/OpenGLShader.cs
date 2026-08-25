using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Engine.Platform.SilkNet;
using Engine.Renderer.Shaders;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLShader : IShader
{
    private uint _handle;
    private readonly Dictionary<string, int> _uniformLocations;
    private bool _disposed;

    public OpenGLShader(string vertPath, string fragPath)
    {
        var vertex = LoadShader(ShaderType.VertexShader, vertPath);
        var fragment = LoadShader(ShaderType.FragmentShader, fragPath);

        _handle = SilkNetContext.GL.CreateProgram();
        OpenGLDebug.CheckError(SilkNetContext.GL, "CreateProgram");

        SilkNetContext.GL.AttachShader(_handle, vertex);
        OpenGLDebug.CheckError(SilkNetContext.GL, "AttachShader(vertex)");
        SilkNetContext.GL.AttachShader(_handle, fragment);
        OpenGLDebug.CheckError(SilkNetContext.GL, "AttachShader(fragment)");

        SilkNetContext.GL.LinkProgram(_handle);
        OpenGLDebug.CheckError(SilkNetContext.GL, "LinkProgram");

        SilkNetContext.GL.GetProgram(_handle, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            throw new InvalidOperationException($"Program failed to link with error: {SilkNetContext.GL.GetProgramInfoLog(_handle)}");
        }

        SilkNetContext.GL.DeleteShader(vertex);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DeleteShader(vertex)");
        SilkNetContext.GL.DeleteShader(fragment);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DeleteShader(fragment)");

        _uniformLocations = new Dictionary<string, int>();

        SilkNetContext.GL.GetProgram(_handle, ProgramPropertyARB.ActiveUniforms, out var numberOfUniforms);

        for (uint i = 0; i < numberOfUniforms; i++)
        {
            var key = SilkNetContext.GL.GetActiveUniform(_handle, i, out _, out _);
            var location = SilkNetContext.GL.GetUniformLocation(_handle, key);
            _uniformLocations.Add(key, location);
        }
    }

    public void Bind()
    {
        SilkNetContext.GL.UseProgram(_handle);
        OpenGLDebug.CheckError(SilkNetContext.GL, "UseProgram");
    }

    public void Unbind()
    {
        SilkNetContext.GL.UseProgram(0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "UseProgram(0)");
    }

    // The shader sources provided with this project use hardcoded layout(location)-s. If you want to do it dynamically,
    // you can omit the layout(location=X) lines in the vertex shader, and use this in VertexAttribPointer instead of the hardcoded values.
    public int GetAttribLocation(string attribName) => SilkNetContext.GL.GetAttribLocation(_handle, attribName);

    /// <summary>
    /// Set a uniform int on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetInt(string name, int data)
    {
        var location = ResolveUniformLocation(name);
        if (location < 0) return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(location, data);
    }

    public void SetIntArray(string name, int[] values, uint count)
    {
        if (!_uniformLocations.TryGetValue(name, out _)) return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(_uniformLocations[name], values);
    }

    public void UploadUniformIntArray(string name, int[] values, uint count) =>
        SetIntArray(name, values, count);

    public void SetFloat(string name, float data)
    {
        var location = ResolveUniformLocation(name);
        if (location < 0) return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(location, data);
    }

    public void SetMat4(string name, Matrix4x4 data)
    {
        var location = ResolveUniformLocation(name);
        if (location < 0) return;
        SilkNetContext.GL.UseProgram(_handle);
        var matrix = MemoryMarshal.CreateReadOnlySpan(ref data.M11, 16);
        SilkNetContext.GL.UniformMatrix4(location, true, matrix);
    }

    public void SetMat4Array(string name, Matrix4x4[] matrices)
    {
        ArgumentNullException.ThrowIfNull(matrices);
        if (matrices.Length == 0)
            return;

        var location = ResolveUniformLocation($"{name}[0]");
        if (location < 0)
            location = ResolveUniformLocation(name);
        if (location < 0)
            return;

        SilkNetContext.GL.UseProgram(_handle);
        var floats = MemoryMarshal.Cast<Matrix4x4, float>(matrices);
        SilkNetContext.GL.UniformMatrix4(location, (uint)matrices.Length, true, floats);
    }

    public void SetFloat3(string name, Vector3 data)
    {
        var location = ResolveUniformLocation(name);
        if (location < 0) return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform3(location, data);
    }

    public void SetFloat4(string name, Vector4 data)
    {
        var location = ResolveUniformLocation(name);
        if (location < 0) return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform4(location, data);
    }

    private int ResolveUniformLocation(string name)
    {
        if (_uniformLocations.TryGetValue(name, out var location))
            return location;

        location = SilkNetContext.GL.GetUniformLocation(_handle, name);
        if (location >= 0)
            _uniformLocations[name] = location;
        return location;
    }

    private static readonly Regex IncludeRegex = new(@"^\s*#include\s+""([^""]+)""\s*$", RegexOptions.Multiline);

    private static string PreprocessIncludes(string filePath, string source, HashSet<string>? visited = null)
    {
        visited ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var baseDir = Path.GetDirectoryName(filePath) ?? ".";

        return IncludeRegex.Replace(source, match =>
        {
            var includePath = Path.GetFullPath(Path.Combine(baseDir, match.Groups[1].Value));
            if (!visited.Add(includePath))
                throw new InvalidOperationException($"Circular shader include detected: {includePath}");

            var included = File.ReadAllText(includePath);
            return PreprocessIncludes(includePath, included, visited);
        });
    }

    private static uint LoadShader(ShaderType type, string path)
    {
        var src = PreprocessIncludes(path, File.ReadAllText(path));

        var handle = SilkNetContext.GL.CreateShader(type);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"CreateShader({type})");
        SilkNetContext.GL.ShaderSource(handle, src);
        OpenGLDebug.CheckError(SilkNetContext.GL, "ShaderSource");
        SilkNetContext.GL.CompileShader(handle);
        OpenGLDebug.CheckError(SilkNetContext.GL, "CompileShader");
        var infoLog = SilkNetContext.GL.GetShaderInfoLog(handle);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new InvalidOperationException($"Error compiling shader of type {type}, failed with error {infoLog}");
        }

        return handle;
    }

    /// <summary>
    /// Releases all resources used by the shader program.
    /// </summary>
    /// <remarks>
    /// This method should be called when the shader is no longer needed to prevent GPU resource leaks.
    /// The shader program handle and uniform location cache will be cleaned up.
    /// Calling this method multiple times is safe due to the disposed flag check.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        _uniformLocations?.Clear();

        try
        {
            if (_handle != 0)
            {
                SilkNetContext.GL.DeleteProgram(_handle);
                _handle = 0;
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete OpenGL shader program {_handle}: {e.Message}");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~OpenGLShader()
    {
        if (!_disposed && _handle != 0)
        {
            Debug.WriteLine(
                $"GPU LEAK: Shader program {_handle} not disposed!"
            );
        }
    }
#endif
}