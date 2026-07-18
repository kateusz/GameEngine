#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in mat3 v_TBN;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 lightColor;
uniform float strength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform vec3 u_ViewPosition;
uniform float u_Shininess;
uniform int u_HasDiffuseMap;
uniform int u_HasSpecularMap;
uniform int u_HasNormalMap;
uniform sampler2D u_DiffuseMap;
uniform sampler2D u_SpecularMap;
uniform sampler2D u_NormalMap;

void main()
{
    vec3 norm;
    if (u_HasNormalMap != 0)
    {
        vec3 sampledNormal = texture(u_NormalMap, v_TexCoord).rgb * 2.0 - 1.0;
        norm = normalize(v_TBN * sampledNormal);
    }
    else
    {
        norm = normalize(v_Normal);
    }

    vec3 diffuseColor = u_HasDiffuseMap != 0
        ? texture(u_DiffuseMap, v_TexCoord).rgb
        : vec3(1.0);
    diffuseColor *= u_Color.rgb;

    vec3 specularColor = u_HasSpecularMap != 0
        ? texture(u_SpecularMap, v_TexCoord).rgb
        : vec3(0.5);

    vec3 ambient = strength * lightColor * diffuseColor;

    vec3 L = normalize(-u_LightDirection);
    float ndotl = max(dot(norm, L), 0.0);
    vec3 diffuse = ndotl * u_LightColor * diffuseColor;

    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 H = normalize(L + V);
    float spec = pow(max(dot(norm, H), 0.0), u_Shininess);
    vec3 specular = spec * u_LightColor * specularColor;

    o_Color = vec4(ambient + diffuse + specular, u_Color.a);
    o_EntityID = u_EntityID;
}
