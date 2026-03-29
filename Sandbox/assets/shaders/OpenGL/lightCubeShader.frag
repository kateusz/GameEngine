#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int o_EntityID;

void main()
{
    o_Color = vec4(1.0);
    o_EntityID = -1;
}
