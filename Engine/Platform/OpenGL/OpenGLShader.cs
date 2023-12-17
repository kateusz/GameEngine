using Engine.Renderer;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Platform.OpenGL;

public class OpenGLShader : IShader
{
    private readonly int _handle;

    private readonly Dictionary<string, int> _uniformLocations;

    public OpenGLShader(string vertPath, string fragPath)
    {
        var shaderSource = File.ReadAllText(vertPath);
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);

        GL.ShaderSource(vertexShader, shaderSource);
        CompileShader(vertexShader);
        
        shaderSource = File.ReadAllText(fragPath);
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, shaderSource);
        CompileShader(fragmentShader);

        _handle = GL.CreateProgram();

        GL.AttachShader(_handle, vertexShader);
        GL.AttachShader(_handle, fragmentShader);

        LinkProgram(_handle);

        // When the shader program is linked, it no longer needs the individual shaders attached to it; the compiled code is copied into the shader program.
        // Detach them, and then delete them.
        // TODO: it's not worth to do this because all debug information will be missing
        //GL.DetachShader(Handle, vertexShader);
        //GL.DetachShader(Handle, fragmentShader);

        GL.DeleteShader(fragmentShader);
        GL.DeleteShader(vertexShader);

        GL.GetProgram(_handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

        _uniformLocations = new Dictionary<string, int>();

        for (var i = 0; i < numberOfUniforms; i++)
        {
            var key = GL.GetActiveUniform(_handle, i, out _, out _);
            var location = GL.GetUniformLocation(_handle, key);
            _uniformLocations.Add(key, location);
        }
    }

    private static void CompileShader(int shader)
    {
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);

        if (code == (int)All.True)
            return;

        // We can use `GL.GetShaderInfoLog(shader)` to get information about the error.
        var infoLog = GL.GetShaderInfoLog(shader);
        throw new Exception($"Error occurred whilst compiling OpenGLShader({shader}).\n\n{infoLog}");
    }

    private static void LinkProgram(int program)
    {
        GL.LinkProgram(program);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
        if (code == (int)All.True)
            return;

        // We can use `GL.GetProgramInfoLog(program)` to get information about the error.
        var errorLog = GL.GetProgramInfoLog(program);
        throw new Exception($"Error occurred whilst linking Program({program}). ${errorLog}");
    }

    public void Bind()
    {
        GL.UseProgram(_handle);
    }

    public void Unbind()
    {
    }

    // The shader sources provided with this project use hardcoded layout(location)-s. If you want to do it dynamically,
    // you can omit the layout(location=X) lines in the vertex shader, and use this in VertexAttribPointer instead of the hardcoded values.
    public int GetAttribLocation(string attribName)
    {
        return GL.GetAttribLocation(_handle, attribName);
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
        GL.UseProgram(_handle);
        GL.Uniform1(_uniformLocations[name], data);
    }

    /// <summary>
    /// Set a uniform float on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetFloat(string name, float data)
    {
        GL.UseProgram(_handle);
        GL.Uniform1(_uniformLocations[name], data);
    }

    /// <summary>
    /// Set a uniform Matrix4 on this shader
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetMat4(string name, Matrix4 data)
    {
        GL.UseProgram(_handle);
        GL.UniformMatrix4(_uniformLocations[name], true, ref data);
    }

    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetFloat3(string name, Vector3 data)
    {
        GL.UseProgram(_handle);
        GL.Uniform3(_uniformLocations[name], data);
    }
    
    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetFloat4(string name, Vector4 data)
    {
        GL.UseProgram(_handle);
        GL.Uniform4(_uniformLocations[name], data);
    }
}