#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 5) in int  a_EntityID;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;

flat out int v_EntityID;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * u_Model;
    v_EntityID = a_EntityID;
    gl_Position = worldPos * u_ViewProjection;
}
