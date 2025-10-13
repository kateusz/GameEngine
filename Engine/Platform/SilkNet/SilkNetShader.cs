using System.Numerics;
using Engine.Renderer.Shaders;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class SilkNetShader : IShader
{
    private readonly uint _handle;
    private readonly Dictionary<string, int> _uniformLocations;

    public SilkNetShader(string vertPath, string fragPath)
    {
        uint vertex = LoadShader(ShaderType.VertexShader, vertPath);
        uint fragment = LoadShader(ShaderType.FragmentShader, fragPath);

        //Create the shader program.
        _handle = SilkNetContext.GL.CreateProgram();

        //Attach the individual shaders.
        SilkNetContext.GL.AttachShader(_handle, vertex);
        SilkNetContext.GL.AttachShader(_handle, fragment);
        SilkNetContext.GL.LinkProgram(_handle);

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
        SilkNetContext.GL.DeleteShader(fragment);

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
    }

    public void Unbind()
    {
        SilkNetContext.GL.UseProgram(0);
    }

    // The shader sources provided with this project use hardcoded layout(location)-s. If you want to do it dynamically,
    // you can omit the layout(location=X) lines in the vertex shader, and use this in VertexAttribPointer instead of the hardcoded values.
    public int GetAttribLocation(string attribName)
    {
        return SilkNetContext.GL.GetAttribLocation(_handle, attribName);
    }

    // Uniform setters
    // Uniforms are variables that can be set by user code, instead of reading them from the VBO.
    // You use VBOs for vertex-related data, and uniforms for almost everything else.

    // Setting a uniform is almost always the exact same, so I'll explain it here once, instead of in every method:
    //     1. Bind the program you want to set the uniform on
    //     2. Get a handle to the location of the uniform with GL.GetUniformLocation.
    //     3. Use the appropriate GL.Uniform* function to set the uniform.

    /// <summary>
    /// Set a uniform int on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetInt(string name, int data)
    {
        int uniformLocation = SilkNetContext.GL.GetUniformLocation(_handle, "u_Texture");

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
    public unsafe void SetMat4(string name, Matrix4x4 data)
    {
        SilkNetContext.GL.UseProgram(_handle);

        // Stack allocation - zero GC pressure
        Span<float> matrix = stackalloc float[16];
        matrix[0] = data.M11;
        matrix[1] = data.M12;
        matrix[2] = data.M13;
        matrix[3] = data.M14;
        matrix[4] = data.M21;
        matrix[5] = data.M22;
        matrix[6] = data.M23;
        matrix[7] = data.M24;
        matrix[8] = data.M31;
        matrix[9] = data.M32;
        matrix[10] = data.M33;
        matrix[11] = data.M34;
        matrix[12] = data.M41;
        matrix[13] = data.M42;
        matrix[14] = data.M43;
        matrix[15] = data.M44;

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

    private static uint LoadShader(ShaderType type, string path)
    {
        //To load a single shader we need to:
        //1) Load the shader from a file.
        //2) Create the handle.
        //3) Upload the source to opengl.
        //4) Compile the shader.
        //5) Check for errors.
        string src = File.ReadAllText(path);
        uint handle = SilkNetContext.GL.CreateShader(type);
        SilkNetContext.GL.ShaderSource(handle, src);
        SilkNetContext.GL.CompileShader(handle);
        string infoLog = SilkNetContext.GL.GetShaderInfoLog(handle);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
        }

        return handle;
    }
}