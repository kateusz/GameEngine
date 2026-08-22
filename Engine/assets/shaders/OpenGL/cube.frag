#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_Normal;
in vec2 v_TexCoord;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 u_AmbientColor;
uniform float u_AmbientStrength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform sampler2D u_Texture;
uniform int u_UseTexture;

void main()
{
    vec4 baseColor = u_UseTexture == 1
        ? texture(u_Texture, v_TexCoord) * u_Color
        : u_Color;
    vec3 albedo = baseColor.rgb;
    vec3 ambient = u_AmbientStrength * u_AmbientColor;

    vec3 N = normalize(v_Normal);
    vec3 L = normalize(-u_LightDirection);
    float ndotl = max(dot(N, L), 0.0);
    vec3 diffuse = ndotl * u_LightColor;

    o_Color = vec4((ambient + diffuse) * albedo, baseColor.a);
    o_EntityID = u_EntityID;
}
