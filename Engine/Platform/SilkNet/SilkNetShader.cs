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
        var shaderSource = File.ReadAllText(vertPath);
        // Create our vertex shader, and give it our vertex shader source code.
        uint vertexShader = SilkNetContext.GL.CreateShader(ShaderType.VertexShader);
        SilkNetContext.GL.ShaderSource(vertexShader, shaderSource);

        // Attempt to compile the shader.
        SilkNetContext.GL.CompileShader(vertexShader);

        // Check to make sure that the shader has successfully compiled.
        SilkNetContext.GL.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
            throw new Exception("Vertex shader failed to compile: " + SilkNetContext.GL.GetShaderInfoLog(vertexShader));

        var fragmentCode = File.ReadAllText(fragPath);
        // Repeat this process for the fragment shader.
        uint fragmentShader = SilkNetContext.GL.CreateShader(ShaderType.FragmentShader);
        SilkNetContext.GL.ShaderSource(fragmentShader, fragmentCode);

        SilkNetContext.GL.CompileShader(fragmentShader);

        SilkNetContext.GL.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
        if (fStatus != (int)GLEnum.True)
            throw new Exception("Fragment shader failed to compile: " +
                                SilkNetContext.GL.GetShaderInfoLog(fragmentShader));

        // Create our shader program, and attach the vertex & fragment shaders.
        _handle = SilkNetContext.GL.CreateProgram();

        SilkNetContext.GL.AttachShader(_handle, vertexShader);
        SilkNetContext.GL.AttachShader(_handle, fragmentShader);

        // Attempt to "link" the program together.
        SilkNetContext.GL.LinkProgram(_handle);

        // Similar to shader compilation, check to make sure that the shader program has linked properly.
        SilkNetContext.GL.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
            throw new Exception("Program failed to link: " + SilkNetContext.GL.GetProgramInfoLog(_handle));

        // Detach and delete our shaders. Once a program is linked, we no longer need the individual shader objects.
        SilkNetContext.GL.DetachShader(_handle, vertexShader);
        SilkNetContext.GL.DetachShader(_handle, fragmentShader);
        SilkNetContext.GL.DeleteShader(vertexShader);
        SilkNetContext.GL.DeleteShader(fragmentShader);

        _uniformLocations = new Dictionary<string, int>();
        
        SilkNetContext.GL.GetProgram(_handle, ProgramPropertyARB.ActiveUniforms, out var numberOfUniforms);

        for (uint i = 0; i < numberOfUniforms; i++)
        {
            var key = SilkNetContext.GL.GetActiveUniform(_handle, i, out _, out _);
            var location = SilkNetContext.GL.GetUniformLocation(_handle, key);
            _uniformLocations.Add(key, location);
        }
    }

    private static void CompileShader(uint shader)
    {
        SilkNetContext.GL.CompileShader(shader);
        SilkNetContext.GL.GetShader(shader, ShaderParameterName.CompileStatus, out var code);

        if (code == (int)GLEnum.True)
            return;

        // We can use `GL.GetShaderInfoLog(shader)` to get information about the error.
        var infoLog = SilkNetContext.GL.GetShaderInfoLog(shader);
        throw new Exception($"Error occurred whilst compiling OpenGLShader({shader}).\n\n{infoLog}");
    }

    private static void LinkProgram(uint program)
    {
        SilkNetContext.GL.LinkProgram(program);
        SilkNetContext.GL.GetProgram(program, ProgramPropertyARB.LinkStatus, out var code);
        if (code == (int)GLEnum.True)
            return;

        // We can use `GL.GetProgramInfoLog(program)` to get information about the error.
        var errorLog = SilkNetContext.GL.GetProgramInfoLog(program);
        throw new Exception($"Error occurred whilst linking Program({program}). ${errorLog}");
    }

    public void Bind()
    {
        SilkNetContext.GL.UseProgram(_handle);
    }

    public void Unbind()
    {
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
        SilkNetContext.GL.UseProgram(_handle);
        SilkNetContext.GL.Uniform1(_uniformLocations[name], data);
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
        ReadOnlySpan<float> matrix = Matrix4x4ToReadOnlySpan(data);

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

    public static ReadOnlySpan<float> Matrix4x4ToReadOnlySpan(Matrix4x4 matrix)
    {
        float[] matrixArray = new float[16]; // Create a float array to hold the matrix elements

        // Copy the elements of the matrix into the array
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

        return new ReadOnlySpan<float>(matrixArray); // Create a ReadOnlySpan<float> from the array
    }
}