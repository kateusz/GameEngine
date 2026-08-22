#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 5) in int  a_EntityID;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform mat4 u_NormalMatrix;
uniform float u_TilingFactor;

out vec3 v_Normal;
out vec2 v_TexCoord;
flat out int v_EntityID;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * u_Model;
    v_Normal = normalize(a_Normal * mat3(u_NormalMatrix));
    v_TexCoord = a_TexCoord * u_TilingFactor;
    v_EntityID = a_EntityID;
    gl_Position = worldPos * u_ViewProjection;
}
