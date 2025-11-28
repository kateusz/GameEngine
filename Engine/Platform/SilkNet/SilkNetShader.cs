using System.Numerics;
using Engine.Renderer.Shaders;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public sealed class SilkNetShader : IShader
{
    private readonly uint _handle;
    private readonly Dictionary<string, int> _uniformLocations;
    private bool _disposed;

    public SilkNetShader(string vertPath, string fragPath)
    {
        var vertex = LoadShader(ShaderType.VertexShader, vertPath);
        var fragment = LoadShader(ShaderType.FragmentShader, fragPath);

        //Create the shader program.
        _handle = SilkNetContext.GL.CreateProgram();
        GLDebug.CheckError(SilkNetContext.GL, "CreateProgram");

        //Attach the individual shaders.
        SilkNetContext.GL.AttachShader(_handle, vertex);
        GLDebug.CheckError(SilkNetContext.GL, "AttachShader(vertex)");
        SilkNetContext.GL.AttachShader(_handle, fragment);
        GLDebug.CheckError(SilkNetContext.GL, "AttachShader(fragment)");
        SilkNetContext.GL.LinkProgram(_handle);
        GLDebug.CheckError(SilkNetContext.GL, "LinkProgram");

        //Check for linking errors.
        SilkNetContext.GL.GetProgram(_handle, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            throw new Exception($"Program failed to link with error: {SilkNetContext.GL.GetProgramInfoLog(_handle)}");
        }

        // do not detach - debug purposes
        //SilkNetContext.GL.DetachShader(_handle, vertexShader);
        //SilkNetContext.GL.DetachShader(_handle, fragmentShader);
        SilkNetContext.GL.DeleteShader(vertex);
        GLDebug.CheckError(SilkNetContext.GL, "DeleteShader(vertex)");
        SilkNetContext.GL.DeleteShader(fragment);
        GLDebug.CheckError(SilkNetContext.GL, "DeleteShader(fragment)");

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
        GLDebug.CheckError(SilkNetContext.GL, "UseProgram");
    }

    public void Unbind()
    {
        SilkNetContext.GL.UseProgram(0);
        GLDebug.CheckError(SilkNetContext.GL, "UseProgram(0)");
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
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(_uniformLocations[name], data);
    }

    public void SetIntArray(string name, int[] values, uint count)
    {
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(_uniformLocations[name], values);
    }

    public void UploadUniformIntArray(string name, int[] values, uint count)
    {
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(_uniformLocations[name], values);
    }

    /// <summary>
    /// Set a uniform float on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetFloat(string name, float data)
    {
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(_uniformLocations[name], data);
    }

    /// <summary>
    /// Set a uniform Matrix4 on this shader
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetMat4(string name, Matrix4x4 data)
    {
        var matrix = Matrix4x4ToReadOnlySpan(data);

        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.UniformMatrix4(_uniformLocations[name], true, matrix);
    }

    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetFloat3(string name, Vector3 data)
    {
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform3(_uniformLocations[name], data);
    }

    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetFloat4(string name, Vector4 data)
    {
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform4(_uniformLocations[name], data);
    }

    private static ReadOnlySpan<float> Matrix4x4ToReadOnlySpan(Matrix4x4 matrix)
    {
        var matrixArray = new float[16];
        
        matrixArray[0] = matrix.M11;
        matrixArray[1] = matrix.M12;
        matrixArray[2] = matrix.M13;
        matrixArray[3] = matrix.M14;
        matrixArray[4] = matrix.M21;
        matrixArray[5] = matrix.M22;
        matrixArray[6] = matrix.M23;
        matrixArray[7] = matrix.M24;
        matrixArray[8] = matrix.M31;
        matrixArray[9] = matrix.M32;
        matrixArray[10] = matrix.M33;
        matrixArray[11] = matrix.M34;
        matrixArray[12] = matrix.M41;
        matrixArray[13] = matrix.M42;
        matrixArray[14] = matrix.M43;
        matrixArray[15] = matrix.M44;

        return new ReadOnlySpan<float>(matrixArray);
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
        GLDebug.CheckError(SilkNetContext.GL, $"CreateShader({type})");
        SilkNetContext.GL.ShaderSource(handle, src);
        GLDebug.CheckError(SilkNetContext.GL, "ShaderSource");
        SilkNetContext.GL.CompileShader(handle);
        GLDebug.CheckError(SilkNetContext.GL, "CompileShader");
        var infoLog = SilkNetContext.GL.GetShaderInfoLog(handle);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    ~SilkNetShader()
    {
        Dispose(false);
    }
    
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _uniformLocations?.Clear();
        }
        
        try
        {
            if (_handle != 0)
            {
                SilkNetContext.GL.DeleteProgram(_handle);
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete OpenGL shader program {_handle}: {e.Message}");
        }

        _disposed = true;
    }
}