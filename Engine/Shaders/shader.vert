#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec4 a_Color;

uniform mat4 u_ViewProjection;
uniform mat4 u_Transform;

out vec3 v_Position;
out vec4 v_Color;

void main(void)
{
    v_Position = a_Position;
    v_Color = a_Color;
    gl_Position =  vec4(a_Position, 1.0) * u_ViewProjection * u_Transform;
}