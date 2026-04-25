#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

const int MAX_POINT_LIGHTS = 16;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in mat3 v_TBN;
flat in int v_EntityID;

uniform int   u_DirectionalLightEnabled;
uniform vec3  u_DirectionalLightDirection;
uniform vec3  u_DirectionalLightColor;
uniform float u_DirectionalLightStrength;

uniform int   u_AmbientLightEnabled;
uniform vec3  u_AmbientLightColor;
uniform float u_AmbientLightStrength;

uniform int   u_PointLightCount;
uniform vec3  u_PointLightPositions[MAX_POINT_LIGHTS];
uniform vec3  u_PointLightColors[MAX_POINT_LIGHTS];
uniform float u_PointLightIntensities[MAX_POINT_LIGHTS];

uniform vec3  u_ViewPosition;
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

    vec3 ambient = vec3(0.0);
    if (u_AmbientLightEnabled != 0)
        ambient = u_AmbientLightColor * u_AmbientLightStrength * diffuseColor;

    vec3 diffuse = vec3(0.0);
    vec3 specular = vec3(0.0);
    vec3 viewDir = normalize(u_ViewPosition - v_FragPos);

    if (u_DirectionalLightEnabled != 0)
    {
        vec3 dirLightDir = normalize(-u_DirectionalLightDirection);
        float dirDiff = max(dot(norm, dirLightDir), 0.0);
        vec3 dirReflectDir = reflect(-dirLightDir, norm);
        float dirSpec = pow(max(dot(viewDir, dirReflectDir), 0.0), u_Shininess);

        diffuse += dirDiff * u_DirectionalLightColor * diffuseColor * u_DirectionalLightStrength;
        specular += dirSpec * u_DirectionalLightColor * specularColor * u_DirectionalLightStrength;
    }

    int pointCount = min(u_PointLightCount, MAX_POINT_LIGHTS);
    for (int i = 0; i < pointCount; i++)
    {
        vec3 pointDir = normalize(u_PointLightPositions[i] - v_FragPos);
        float pointDiff = max(dot(norm, pointDir), 0.0);
        vec3 pointReflectDir = reflect(-pointDir, norm);
        float pointSpec = pow(max(dot(viewDir, pointReflectDir), 0.0), u_Shininess);

        vec3 pointColor = u_PointLightColors[i] * u_PointLightIntensities[i];
        diffuse += pointDiff * pointColor * diffuseColor;
        specular += pointSpec * pointColor * specularColor;
    }

    o_Color    = vec4(ambient + diffuse + specular, u_Color.a);
    o_EntityID = u_EntityID;
}
