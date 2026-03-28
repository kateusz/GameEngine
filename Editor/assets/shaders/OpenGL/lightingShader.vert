#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;

out vec3 v_Position;
out vec3 v_Normal;

void main()
{
    v_Position = vec3(vec4(a_Position, 1.0) * u_Model);
    v_Normal   = vec3(vec4(a_Normal,   0.0) * u_Model);
    gl_Position = vec4(a_Position, 1.0) * u_Model * u_ViewProjection;
}
