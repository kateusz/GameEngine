using System.Diagnostics;
using System.Numerics;
using Engine.Platform.SilkNet;
using Engine.Renderer.Shaders;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLShader : IShader
{
    private static readonly ILogger Logger = Log.ForContext<OpenGLShader>();
    private static readonly HashSet<uint> LoggedMissingBoneUniformPrograms = [];

    private uint _handle;
    private readonly Dictionary<string, int> _uniformLocations;
    private readonly Dictionary<string, int[]> _arrayElementLocations = new(StringComparer.Ordinal);
    private bool _disposed;

    public OpenGLShader(string vertPath, string fragPath)
    {
        var vertex = LoadShader(ShaderType.VertexShader, vertPath);
        var fragment = LoadShader(ShaderType.FragmentShader, fragPath);

        //Create the shader program.
        _handle = SilkNetContext.GL.CreateProgram();
        OpenGLDebug.CheckError(SilkNetContext.GL, "CreateProgram");

        //Attach the individual shaders.
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
        if (!_uniformLocations.TryGetValue(name, out var location)) return;
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
        if (!_uniformLocations.TryGetValue(name, out var location)) return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(location, data);
    }

    public void SetMat4(string name, Matrix4x4 data)
    {
        if (!TryGetUniformLocation(name, out var location)) return;
        var matrix = Matrix4x4ToReadOnlySpan(data);
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.UniformMatrix4(location, true, matrix);
    }

    public void SetMat4Array(string name, Matrix4x4[] values, uint count)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (count > values.Length)
            throw new ArgumentOutOfRangeException(nameof(count), count, $"count ({count}) exceeds values.Length ({values.Length})");
        if (!TryGetUniformLocation(name, out var location) || count == 0)
        {
            if (name.Contains("BoneMatrices", StringComparison.Ordinal)
                && LoggedMissingBoneUniformPrograms.Add(_handle))
            {
                var keys = string.Join(", ", _uniformLocations.Keys.OrderBy(k => k, StringComparer.Ordinal));
                Logger.Warning(
                    "SkinnedDbg SetMat4Array missed uniform {Uniform} on program {Program}; active uniforms: {Keys}",
                    name, _handle, keys);
            }

            return;
        }

        // Pack exactly `count` matrices (do not ignore count — SetIntArray anti-pattern).
        var packed = new float[count * 16];
        for (uint i = 0; i < count; i++)
            PackMatrix4x4(values[i], packed, (int)(i * 16));

        // Element locations queried from the driver — the stride is not portable:
        // Apple GL assigns 4 locations per mat4 array element, conformant drivers assign 1.
        // Per-element single-matrix uploads also dodge the macOS glUniformMatrix4fv(count>1) quirk.
        var locations = GetArrayElementLocations(name, location, count);

        SilkNetContext.GL.UseProgram(_handle);
        unsafe
        {
            for (uint i = 0; i < count; i++)
            {
                if (locations[i] < 0)
                    continue;
                fixed (float* ptr = &packed[i * 16])
                    SilkNetContext.GL.UniformMatrix4(locations[i], 1, true, ptr);
            }
        }
        OpenGLDebug.CheckError(SilkNetContext.GL, "UniformMatrix4(array)");
    }

    private int[] GetArrayElementLocations(string name, int baseLocation, uint count)
    {
        var baseName = name.EndsWith("[0]", StringComparison.Ordinal) ? name[..^3] : name;
        if (_arrayElementLocations.TryGetValue(baseName, out var cached) && cached.Length >= count)
            return cached;

        var locations = new int[count];
        locations[0] = baseLocation;
        for (uint i = 1; i < count; i++)
            locations[i] = SilkNetContext.GL.GetUniformLocation(_handle, $"{baseName}[{i}]");

        _arrayElementLocations[baseName] = locations;
        return locations;
    }

    internal void LogUniformInventory(string label)
    {
        var rows = _uniformLocations
            .OrderBy(kv => kv.Value)
            .Select(kv => $"{kv.Key}@{kv.Value}");
        Logger.Information(
            "SkinnedDbg shader uniforms {Label} program={Program}: {Uniforms}",
            label, _handle, string.Join(", ", rows));
    }

    private bool TryGetUniformLocation(string name, out int location)
    {
        if (_uniformLocations.TryGetValue(name, out location))
            return true;

        // GLSL array uniforms: drivers may register "u_BoneMatrices" or "u_BoneMatrices[0]".
        if (name.EndsWith("[0]", StringComparison.Ordinal))
        {
            var baseName = name[..^3];
            if (_uniformLocations.TryGetValue(baseName, out location))
                return true;
        }
        else if (_uniformLocations.TryGetValue(name + "[0]", out location))
            return true;

        location = -1;
        return false;
    }

    public void SetFloat3(string name, Vector3 data)
    {
        if (!_uniformLocations.TryGetValue(name, out var location)) return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform3(location, data);
    }

    public void SetFloat4(string name, Vector4 data)
    {
        if (!_uniformLocations.TryGetValue(name, out var location)) return;
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform4(location, data);
    }

    private static ReadOnlySpan<float> Matrix4x4ToReadOnlySpan(Matrix4x4 matrix)
    {
        var matrixArray = new float[16];
        PackMatrix4x4(matrix, matrixArray, 0);
        return new ReadOnlySpan<float>(matrixArray);
    }

    private static void PackMatrix4x4(Matrix4x4 matrix, float[] destination, int offset)
    {
        destination[offset] = matrix.M11;
        destination[offset + 1] = matrix.M12;
        destination[offset + 2] = matrix.M13;
        destination[offset + 3] = matrix.M14;
        destination[offset + 4] = matrix.M21;
        destination[offset + 5] = matrix.M22;
        destination[offset + 6] = matrix.M23;
        destination[offset + 7] = matrix.M24;
        destination[offset + 8] = matrix.M31;
        destination[offset + 9] = matrix.M32;
        destination[offset + 10] = matrix.M33;
        destination[offset + 11] = matrix.M34;
        destination[offset + 12] = matrix.M41;
        destination[offset + 13] = matrix.M42;
        destination[offset + 14] = matrix.M43;
        destination[offset + 15] = matrix.M44;
    }

    private static uint LoadShader(ShaderType type, string path)
    {
        //To load a single shader we need to:
        //1) Load the shader from a file.
        //2) Create the handle.
        //3) Upload the source to opengl.
        //4) Compile the shader.
        //5) Check for errors.
        var src = File.ReadAllText(path);
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