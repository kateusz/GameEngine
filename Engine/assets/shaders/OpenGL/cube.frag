#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in vec4 v_FragPosLightSpace;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 u_AmbientColor;
uniform float u_AmbientStrength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform vec3 u_ViewPosition;
uniform sampler2D u_Texture;
uniform sampler2D u_ShadowMap;
uniform samplerCube u_PointShadowMap;
uniform int u_UseTexture;
uniform int u_ShadowsEnabled;
uniform int u_PointShadowsEnabled;

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
    vec4 baseColor = u_UseTexture == 1
        ? texture(u_Texture, v_TexCoord) * u_Color
        : u_Color;
    vec3 albedo = baseColor.rgb;
    vec3 ambient = u_AmbientStrength * u_AmbientColor;

    vec3 N = normalize(v_Normal);
    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 L = normalize(-u_LightDirection);
    float ndotl = max(dot(N, L), 0.0);
    float shadow = u_ShadowsEnabled != 0 ? ShadowCalculation(N, L) : 0.0;
    vec3 diffuse = (1.0 - shadow) * ndotl * u_LightColor;

    vec3 lit = ambient + diffuse;
    for (int i = 0; i < u_PointLightCount; i++)
    {
        vec3 contribution = PointContribution(u_PointLights[i].position, u_PointLights[i].color,
            u_PointLights[i].constant, u_PointLights[i].linear, u_PointLights[i].quadratic, u_PointLights[i].range,
            N, V, v_FragPos, vec3(1.0), vec3(0.0), 1.0);
        if (i == 0 && u_PointShadowsEnabled != 0)
        {
            contribution *= 1.0 - PointShadowPCF(v_FragPos, N, u_PointLights[0].position, u_PointLights[0].range);
        }
        lit += contribution;
    }
    for (int i = 0; i < u_SpotLightCount; i++)
    {
        lit += PointContribution(u_SpotLights[i].position, u_SpotLights[i].color,
            u_SpotLights[i].constant, u_SpotLights[i].linear, u_SpotLights[i].quadratic, 0.0,
            N, V, v_FragPos, vec3(1.0), vec3(0.0), 1.0)
            * SpotCone(u_SpotLights[i].position, u_SpotLights[i].direction, v_FragPos,
                u_SpotLights[i].innerCos, u_SpotLights[i].outerCos);
    }

    o_Color = vec4(lit * albedo, baseColor.a);
    o_EntityID = u_EntityID;
}
