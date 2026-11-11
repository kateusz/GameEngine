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
    private readonly Dictionary<int, Vector2> _float2Uniforms = new();
#if DEBUG
    private readonly HashSet<string> _missingUniforms = new();
#endif
    private bool _disposed;

    internal uint RendererId => _handle;

    public OpenGLShader(string vertPath, string fragPath)
    {
        var dir = Path.GetDirectoryName(vertPath)!;
        CompileFromSource(
            ExpandIncludes(File.ReadAllText(vertPath), dir),
            ExpandIncludes(File.ReadAllText(fragPath), dir));
        BindFrameBlock();
        CacheUniforms();
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
        SilkNetContext.GL.ProgramUniform1(_handle, location, data);
    }

    public void SetIntArray(string name, int[] values, uint count)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        SilkNetContext.GL.ProgramUniform1(_handle, location, values.AsSpan(0, (int)count));
    }

    public void SetFloat(string name, float data)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        SilkNetContext.GL.ProgramUniform1(_handle, location, data);
    }

    public void SetMat4(string name, Matrix4x4 data)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        var matrix = MemoryMarshal.CreateReadOnlySpan(ref data.M11, 16);
        SilkNetContext.GL.ProgramUniformMatrix4(_handle, location, true, matrix);
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

        var floats = MemoryMarshal.Cast<Matrix4x4, float>(matrices);
        SilkNetContext.GL.ProgramUniformMatrix4(_handle, location, (uint)matrices.Length, true, floats);
    }

    public void SetFloat2(string name, Vector2 data)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        if (_float2Uniforms.TryGetValue(location, out var previous) && previous == data)
            return;

        SilkNetContext.GL.ProgramUniform2(_handle, location, data);
        _float2Uniforms[location] = data;
    }

    public void SetFloat3(string name, Vector3 data)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        SilkNetContext.GL.ProgramUniform3(_handle, location, data);
    }

    public void SetFloat4(string name, Vector4 data)
    {
        if (!TryGetUniformLocation(name, out var location))
            return;
        SilkNetContext.GL.ProgramUniform4(_handle, location, data);
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

    internal static string ExpandIncludes(string src, string directory)
    {
        const string include = "#include \"";
        while (true)
        {
            var at = src.IndexOf(include, StringComparison.Ordinal);
            if (at < 0)
                return src;
            var nameAt = at + include.Length;
            var end = src.IndexOf('"', nameAt);
            if (end < 0)
                throw new InvalidOperationException("Unterminated #include");
            var included = File.ReadAllText(Path.Combine(directory, src[nameAt..end]));
            src = src.Remove(at, end - at + 1).Insert(at, included);
        }
    }

    private void CompileFromSource(string vertSrc, string fragSrc)
    {
        uint vertex = 0;
        uint fragment = 0;
        try
        {
            vertex = CompileStage(ShaderType.VertexShader, vertSrc);
            fragment = CompileStage(ShaderType.FragmentShader, fragSrc);

            _handle = SilkNetContext.GL.CreateProgram();
            OpenGLDebug.CheckError(SilkNetContext.GL, "CreateProgram");
            SilkNetContext.GL.AttachShader(_handle, vertex);
            OpenGLDebug.CheckError(SilkNetContext.GL, "AttachShader(vertex)");
            SilkNetContext.GL.AttachShader(_handle, fragment);
            OpenGLDebug.CheckError(SilkNetContext.GL, "AttachShader(fragment)");
            SilkNetContext.GL.LinkProgram(_handle);
            OpenGLDebug.CheckError(SilkNetContext.GL, "LinkProgram");
            EnsureLinked();

            SilkNetContext.GL.DeleteShader(vertex);
            vertex = 0;
            SilkNetContext.GL.DeleteShader(fragment);
            fragment = 0;
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

    private void EnsureLinked()
    {
        SilkNetContext.GL.GetProgram(_handle, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            throw new InvalidOperationException(
                $"Program failed to link with error: {SilkNetContext.GL.GetProgramInfoLog(_handle)}");
        }
    }

    private void BindFrameBlock()
    {
        var block = SilkNetContext.GL.GetUniformBlockIndex(_handle, "Frame");
        if (block != uint.MaxValue)
            SilkNetContext.GL.UniformBlockBinding(_handle, block, 0);
    }

    private void CacheUniforms()
    {
        SilkNetContext.GL.GetProgram(_handle, ProgramPropertyARB.ActiveUniforms, out var numberOfUniforms);
        for (uint i = 0; i < numberOfUniforms; i++)
        {
            var key = SilkNetContext.GL.GetActiveUniform(_handle, i, out _, out _);
            var location = SilkNetContext.GL.GetUniformLocation(_handle, key);
            _uniformLocations[key] = location;
        }
    }

    private static uint CompileStage(ShaderType type, string src)
    {
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
            Debug.WriteLine($"Failed to delete OpenGL shader program {_handle}: {e.Message}");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~OpenGLShader()
    {
        if (!_disposed && _handle != 0)
            Debug.WriteLine($"GPU LEAK: Shader program {_handle} not disposed!");
    }
#endif
}
