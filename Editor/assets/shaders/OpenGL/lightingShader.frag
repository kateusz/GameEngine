#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_Position;
in vec3 v_Normal;

uniform vec3 u_LightPosition;
uniform vec3 u_LightColor;
uniform vec3 u_Color;
uniform int  u_EntityID;

void main()
{
    // ambient
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * u_LightColor;

    // diffuse
    vec3 norm     = normalize(v_Normal);
    vec3 lightDir = normalize(u_LightPosition - v_Position);
    float diff    = max(dot(norm, lightDir), 0.0);
    vec3 diffuse  = diff * u_LightColor;

    o_Color    = vec4((ambient + diffuse) * u_Color, 1.0);
    o_EntityID = u_EntityID;
}
