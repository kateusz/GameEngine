using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Platform.SilkNet;
using Engine.Renderer.Shaders;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLShader : IShader
{
    private uint _handle;
    private readonly Dictionary<string, int> _uniformLocations = new();
#if DEBUG
    private readonly HashSet<string> _missingUniforms = new();
#endif
    private bool _disposed;

    internal uint RendererId => _handle;

    public OpenGLShader(string vertPath, string fragPath)
    {
        uint vertex = 0;
        uint fragment = 0;
        try
        {
            vertex = LoadShader(ShaderType.VertexShader, vertPath);
            fragment = LoadShader(ShaderType.FragmentShader, fragPath);

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
                throw new InvalidOperationException(
                    $"Program failed to link with error: {SilkNetContext.GL.GetProgramInfoLog(_handle)}");
            }

            SilkNetContext.GL.DeleteShader(vertex);
            OpenGLDebug.CheckError(SilkNetContext.GL, "DeleteShader(vertex)");
            vertex = 0;
            SilkNetContext.GL.DeleteShader(fragment);
            OpenGLDebug.CheckError(SilkNetContext.GL, "DeleteShader(fragment)");
            fragment = 0;

            SilkNetContext.GL.GetProgram(_handle, ProgramPropertyARB.ActiveUniforms, out var numberOfUniforms);

            for (uint i = 0; i < numberOfUniforms; i++)
            {
                var key = SilkNetContext.GL.GetActiveUniform(_handle, i, out _, out _);
                var location = SilkNetContext.GL.GetUniformLocation(_handle, key);
                _uniformLocations.Add(key, location);
            }
        }
        catch
        {
            DeleteShaderIfNeeded(vertex);
            DeleteShaderIfNeeded(fragment);
            if (_handle != 0)
            {
                SilkNetContext.GL.DeleteProgram(_handle);
                _handle = 0;
            }

            throw;
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

    public void SetInt(string name, int data)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(location, data);
    }

    public void SetIntArray(string name, int[] values, uint count)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(location, values);
    }

    public void SetFloat(string name, float data)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(location, data);
    }

    public void SetMat4(string name, Matrix4x4 data)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
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
        {
#if DEBUG
            if (_missingUniforms.Add(name))
                Debug.WriteLine($"Missing shader uniform '{name}'");
#endif
            return;
        }

        SilkNetContext.GL.UseProgram(_handle);
        var floats = MemoryMarshal.Cast<Matrix4x4, float>(matrices);
        SilkNetContext.GL.UniformMatrix4(location, (uint)matrices.Length, true, floats);
    }

    public void SetFloat3(string name, Vector3 data)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform3(location, data);
    }

    public void SetFloat4(string name, Vector4 data)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform4(location, data);
    }

    private bool TryGetUniformLocation(string name, out int location)
    {
        location = ResolveUniformLocation(name);
        if (location >= 0)
            return true;
#if DEBUG
        if (_missingUniforms.Add(name))
            Debug.WriteLine($"Missing shader uniform '{name}'");
#endif
        return false;
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

    private static uint LoadShader(ShaderType type, string path)
    {
        var src = File.ReadAllText(path);

        var handle = SilkNetContext.GL.CreateShader(type);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"CreateShader({type})");
        try
        {
            SilkNetContext.GL.ShaderSource(handle, src);
            OpenGLDebug.CheckError(SilkNetContext.GL, "ShaderSource");
            SilkNetContext.GL.CompileShader(handle);
            OpenGLDebug.CheckError(SilkNetContext.GL, "CompileShader");
            var infoLog = SilkNetContext.GL.GetShaderInfoLog(handle);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new InvalidOperationException(
                    $"Error compiling shader of type {type}, failed with error {infoLog}");
            }

            return handle;
        }
        catch
        {
            DeleteShaderIfNeeded(handle);
            throw;
        }
    }

    private static void DeleteShaderIfNeeded(uint handle)
    {
        if (handle == 0)
            return;

        SilkNetContext.GL.DeleteShader(handle);
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