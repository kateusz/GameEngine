#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in vec4 v_FragPosLightSpace;
in mat3 v_TBN;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 u_AmbientColor;
uniform float u_AmbientStrength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform vec3 u_ViewPosition;
uniform float u_Shininess;
uniform int u_HasDiffuseMap;
uniform int u_HasSpecularMap;
uniform int u_HasNormalMap;
uniform int u_ShadowsEnabled;
uniform int u_PointShadowsEnabled;
uniform sampler2D u_DiffuseMap;
uniform sampler2D u_SpecularMap;
uniform sampler2D u_NormalMap;
uniform sampler2D u_ShadowMap;
uniform samplerCube u_PointShadowMap;

struct PointLight {
    vec3 position;
    vec3 color;
    float constant;
    float linear;
    float quadratic;
    float range;
};
uniform PointLight u_PointLights[4]; // keep in sync with LightingMath.MaxPointLights
uniform int u_PointLightCount;

struct SpotLight {
    vec3 position;
    vec3 direction;
    vec3 color;
    float constant;
    float linear;
    float quadratic;
    float innerCos;
    float outerCos;
};
uniform SpotLight u_SpotLights[2]; // keep in sync with LightingMath.MaxSpotLights
uniform int u_SpotLightCount;

float ShadowCalculation(vec3 normal, vec3 lightDir)
{
    vec3 projCoords = v_FragPosLightSpace.xyz / v_FragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    if (projCoords.z > 1.0)
        return 0.0;

    float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);
    float shadow = 0.0;
    vec2 texelSize = 1.0 / vec2(textureSize(u_ShadowMap, 0));
    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(u_ShadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += projCoords.z - bias > pcfDepth ? 1.0 : 0.0;
        }
    }
    return shadow / 9.0;
}

#include "pointShadowPCF.glsl"

vec3 PointContribution(vec3 lightPos, vec3 lightColor, float constant, float linear, float quadratic, float range,
                       vec3 N, vec3 V, vec3 fragPos, vec3 diffuseColor, vec3 specularColor, float shininess)
{
    vec3 toLight = lightPos - fragPos;
    float distance = length(toLight);
    if (range > 0.0 && distance > range)
        return vec3(0.0);
    vec3 L = toLight / max(distance, 1e-4);
    float attenuation = 1.0 / max(constant + linear * distance + quadratic * distance * distance, 1e-4);
    float rangeFade = range > 0.0
        ? clamp(1.0 - pow(distance / range, 4.0), 0.0, 1.0)
        : 1.0;
    float ndotl = max(dot(N, L), 0.0);
    vec3 H = normalize(L + V);
    float spec = pow(max(dot(N, H), 0.0), shininess);
    return (ndotl * lightColor * diffuseColor + spec * lightColor * specularColor) * attenuation * rangeFade;
}

float SpotCone(vec3 lightPos, vec3 spotDir, vec3 fragPos, float innerCos, float outerCos)
{
    vec3 lightDir = normalize(lightPos - fragPos);
    float theta = dot(lightDir, normalize(-spotDir));
    return clamp((theta - outerCos) / max(innerCos - outerCos, 1e-4), 0.0, 1.0);
}

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

    vec3 ambient = u_AmbientStrength * u_AmbientColor * diffuseColor;

    vec3 L = normalize(-u_LightDirection);
    float ndotl = max(dot(norm, L), 0.0);
    vec3 diffuse = ndotl * u_LightColor * diffuseColor;

    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 H = normalize(L + V);
    float spec = pow(max(dot(norm, H), 0.0), u_Shininess);
    vec3 specular = spec * u_LightColor * specularColor;

    float shadow = u_ShadowsEnabled != 0 ? ShadowCalculation(norm, L) : 0.0;
    vec3 directional = (1.0 - shadow) * (diffuse + specular);

    vec3 color = ambient + directional;
    for (int i = 0; i < u_PointLightCount; i++)
    {
        vec3 contribution = PointContribution(u_PointLights[i].position, u_PointLights[i].color,
            u_PointLights[i].constant, u_PointLights[i].linear, u_PointLights[i].quadratic, u_PointLights[i].range,
            norm, V, v_FragPos, diffuseColor, specularColor, u_Shininess);
        if (i == 0 && u_PointShadowsEnabled != 0)
        {
            contribution *= 1.0 - PointShadowPCF(v_FragPos, v_Normal, u_PointLights[0].position, u_PointLights[0].range);
        }
        color += contribution;
    }
    for (int i = 0; i < u_SpotLightCount; i++)
    {
        color += PointContribution(u_SpotLights[i].position, u_SpotLights[i].color,
            u_SpotLights[i].constant, u_SpotLights[i].linear, u_SpotLights[i].quadratic, 0.0,
            norm, V, v_FragPos, diffuseColor, specularColor, u_Shininess)
            * SpotCone(u_SpotLights[i].position, u_SpotLights[i].direction, v_FragPos,
                u_SpotLights[i].innerCos, u_SpotLights[i].outerCos);
    }

    o_Color = vec4(color, u_Color.a);
    o_EntityID = u_EntityID;
}
