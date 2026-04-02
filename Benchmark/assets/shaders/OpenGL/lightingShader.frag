#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in mat3 v_TBN;
flat in int v_EntityID;

uniform vec3  u_LightPosition;
uniform vec3  u_LightDirection;
uniform int   u_LightType;
uniform vec3  u_ViewPosition;
uniform vec3  u_LightColor;
uniform vec4  u_Color;
uniform float u_Shininess;
uniform int   u_HasDiffuseMap;
uniform int   u_HasSpecularMap;
uniform int   u_HasNormalMap;
uniform int   u_EntityID;
uniform sampler2D u_DiffuseMap;
uniform sampler2D u_SpecularMap;
uniform sampler2D u_NormalMap;

void main()
{
    vec3 norm;
    if (u_HasNormalMap != 0)
    {
        vec3 sampledNormal = texture(u_NormalMap, v_TexCoord).rgb;
        sampledNormal = sampledNormal * 2.0 - 1.0;
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

    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * u_LightColor * diffuseColor;

    vec3 lightDir = (u_LightType == 1)
        ? normalize(-u_LightDirection)
        : normalize(u_LightPosition - v_FragPos);
    float diff    = max(dot(norm, lightDir), 0.0);
    vec3 diffuse  = diff * u_LightColor * diffuseColor;

    vec3 viewDir    = normalize(u_ViewPosition - v_FragPos);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec      = pow(max(dot(viewDir, reflectDir), 0.0), u_Shininess);
    vec3 specular   = spec * u_LightColor * specularColor;

    o_Color    = vec4(ambient + diffuse + specular, u_Color.a);
    o_EntityID = u_EntityID;
}
