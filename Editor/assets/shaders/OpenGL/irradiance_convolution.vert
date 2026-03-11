#version 330 core

layout(location = 0) in vec3 a_Position;

out vec3 v_LocalPos;

uniform mat4 u_Projection;
uniform mat4 u_View;

void main()
{
    v_LocalPos = a_Position;
    gl_Position = vec4(a_Position, 1.0) * u_View * u_Projection;
}
