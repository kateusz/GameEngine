#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

flat in int v_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;

void main()
{
    o_Color = u_Color;
    o_EntityID = u_EntityID;
}
