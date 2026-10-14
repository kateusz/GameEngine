#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform mat4 u_NormalMatrix;
uniform mat4 u_View;

out vec3 v_ViewNormal;
out float v_ViewZ;
out vec2 v_TexCoord;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * u_Model;
    vec3 worldNormal = normalize(a_Normal * mat3(u_NormalMatrix));
    v_ViewNormal = normalize(worldNormal * mat3(u_View));
    v_ViewZ = (worldPos * u_View).z;
    v_TexCoord = a_TexCoord;
    gl_Position = worldPos * u_ViewProjection;
}
