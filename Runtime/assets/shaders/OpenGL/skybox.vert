#version 330 core

layout(location = 0) in vec3 a_Position;

uniform mat4 u_ViewProjection;

out vec3 v_Dir;

void main()
{
    v_Dir = a_Position;
    vec4 pos = vec4(a_Position, 1.0) * u_ViewProjection;
    gl_Position = pos.xyww;
}
