#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 lightColor;
uniform float strength;

void main()
{
    vec3 ambient = strength * lightColor;
    vec3 result = ambient * u_Color.rgb;

    o_Color = vec4(result, u_Color.a);
    o_EntityID = u_EntityID;
}
