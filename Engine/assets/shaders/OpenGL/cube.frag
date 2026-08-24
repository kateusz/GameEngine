#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 u_AmbientColor;
uniform float u_AmbientStrength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform vec3 u_ViewPosition;
uniform float u_Shininess;
uniform sampler2D u_Texture;
uniform int u_UseTexture;

void main()
{
    vec4 baseColor = u_UseTexture == 1
        ? texture(u_Texture, v_TexCoord) * u_Color
        : u_Color;
    vec3 albedo = baseColor.rgb;
    vec3 ambient = u_AmbientStrength * u_AmbientColor * albedo;

    vec3 N = normalize(v_Normal);
    vec3 L = normalize(-u_LightDirection);
    float ndotl = max(dot(N, L), 0.0);
    vec3 diffuse = ndotl * u_LightColor * albedo;

    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 H = normalize(L + V);
    float spec = pow(max(dot(N, H), 0.0), u_Shininess);
    vec3 specular = spec * u_LightColor * vec3(0.5);

    o_Color = vec4(ambient + diffuse + specular, baseColor.a);
    o_EntityID = u_EntityID;
}
