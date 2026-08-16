#version 330 core

layout(location = 0) in vec3 a_Position;

uniform mat4 u_ViewProjection;

out vec3 v_LocalPos;

void main()
{
    v_LocalPos = a_Position;
    gl_Position = vec4(a_Position, 1.0) * u_ViewProjection;
}
