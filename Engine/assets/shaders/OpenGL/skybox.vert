#version 330 core

layout(location = 0) in vec3 a_Position;

out vec3 v_Direction;

uniform mat4 u_ViewProjection;
uniform float u_Scale;

void main()
{
    v_Direction = a_Position;
    gl_Position = vec4(a_Position * u_Scale, 1.0) * u_ViewProjection;
}