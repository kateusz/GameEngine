#version 330 core

layout(location = 0) out vec4 color;
layout(location = 1) out int color2;

in vec4 v_Color;
flat in int v_EntityID;

void main()
{
    color = v_Color;
    color2 = v_EntityID;
}