#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_Normal;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 lightColor;
uniform float strength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;

void main()
{
    vec3 albedo = u_Color.rgb;
    vec3 ambient = strength * lightColor;

    vec3 N = normalize(v_Normal);
    vec3 L = normalize(-u_LightDirection);
    float ndotl = max(dot(N, L), 0.0);
    vec3 diffuse = ndotl * u_LightColor;

    o_Color = vec4((ambient + diffuse) * albedo, u_Color.a);
    o_EntityID = u_EntityID;
}
