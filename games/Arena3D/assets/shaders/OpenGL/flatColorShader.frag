#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_Normal;
in vec3 v_FragPos;
in vec4 v_FragPosLightSpace;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 lightColor;
uniform float strength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform sampler2D u_ShadowMap;
uniform int u_ShadowsEnabled;
uniform vec3 u_PointLightPosition;
uniform vec3 u_PointLightColor;
uniform float u_PointLightRange;
uniform samplerCube u_PointShadowMap;
uniform int u_PointShadowsEnabled;

float ShadowCalculation(vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    float currentDepth = projCoords.z;
    float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(u_ShadowMap, 0);
    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(u_ShadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;
        }
    }
    shadow /= 9.0;
    if (projCoords.z > 1.0)
        shadow = 0.0;
    return shadow;
}

float PointShadowCalculation(vec3 fragPos)
{
    vec3 fragToLight = fragPos - u_PointLightPosition;
    float closestDepth = texture(u_PointShadowMap, fragToLight).r * u_PointLightRange;
    float currentDepth = length(fragToLight);
    float bias = 0.05;
    return currentDepth - bias > closestDepth ? 1.0 : 0.0;
}

void main()
{
    vec3 albedo = u_Color.rgb;
    vec3 ambient = strength * lightColor;

    vec3 N = normalize(v_Normal);
    vec3 L = normalize(-u_LightDirection);
    float ndotl = max(dot(N, L), 0.0);
    vec3 diffuse = ndotl * u_LightColor;
    float shadow = u_ShadowsEnabled != 0 ? ShadowCalculation(v_FragPosLightSpace, N, L) : 0.0;

    vec3 toPoint = u_PointLightPosition - v_FragPos;
    float pointDistance = length(toPoint);
    vec3 pointDiffuse = vec3(0.0);
    if (u_PointLightColor != vec3(0.0) && pointDistance < u_PointLightRange)
    {
        vec3 Lp = toPoint / max(pointDistance, 1e-4);
        float att = 1.0 / max(pointDistance * pointDistance, 1e-4);
        float pShadow = u_PointShadowsEnabled != 0 ? PointShadowCalculation(v_FragPos) : 0.0;
        pointDiffuse = (1.0 - pShadow) * max(dot(N, Lp), 0.0) * u_PointLightColor * att;
    }

    o_Color = vec4((ambient + (1.0 - shadow) * diffuse + pointDiffuse) * albedo, u_Color.a);
    o_EntityID = u_EntityID;
}
