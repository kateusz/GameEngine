#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform mat4 u_NormalMatrix;

out vec3 v_Normal;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * u_Model;
    v_Normal = normalize(a_Normal * mat3(u_NormalMatrix));
    gl_Position = worldPos * u_ViewProjection;
}
