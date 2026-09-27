#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 6) in mat4 a_InstanceModel;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform int u_Instanced;

void main()
{
    mat4 model = u_Instanced != 0 ? a_InstanceModel : u_Model;
    vec4 worldPos = vec4(a_Position, 1.0) * model;
    gl_Position = worldPos * u_ViewProjection;
}
