#version 330 core

layout(location = 0) in vec3 a_Position;


uniform mat4 u_ViewProjection;
uniform mat4 u_Transform;

void main()
{
    gl_Position =  vec4(a_Position, 1.0) * u_Transform * u_ViewProjection;
}