#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 3) in int a_EntityID;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform mat4 u_NormalMatrix;

out vec3 v_Position;
out vec3 v_Normal;
out vec2 v_TexCoord;
flat out int v_EntityID;

void main()
{
    v_Position = vec3(u_Model * vec4(a_Position, 1.0));
    v_Normal = normalize(mat3(u_NormalMatrix) * a_Normal);
    v_TexCoord = a_TexCoord;
    v_EntityID = a_EntityID;

    // macOS version
    gl_Position = vec4(a_Position, 1.0) * u_Model * u_ViewProjection;
}