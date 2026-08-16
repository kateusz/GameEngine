#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec4 a_Color;
layout(location = 2) in int a_EntityID;

uniform mat4 u_ViewProjection;

out vec4 v_Color;
flat out int v_EntityID;

void main()
{
    v_Color = a_Color;
    v_EntityID = a_EntityID;

    gl_Position = vec4(a_Position, 1.0) * u_ViewProjection;
}