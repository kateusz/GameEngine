#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 3) in vec3 a_Tangent;
layout(location = 4) in vec3 a_Bitangent;
layout(location = 5) in int a_EntityID;

uniform mat4 u_LightSpaceMatrix;
uniform mat4 u_Model;

void main()
{
    // macOS: row-vector multiplication to match System.Numerics row-major matrices
    gl_Position = vec4(a_Position, 1.0) * u_Model * u_LightSpaceMatrix;
}
