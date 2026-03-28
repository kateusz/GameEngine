#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int o_EntityID;

uniform vec3 objectColor;
uniform vec3 lightColor;
uniform int u_EntityID;

void main()
{
    o_Color = vec4(lightColor * objectColor, 1.0);
    o_EntityID = u_EntityID;
}
