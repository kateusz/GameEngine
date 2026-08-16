#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in mat3 v_TBN;
in vec4 v_FragPosLightSpace;

uniform vec4 u_Color;
uniform int  u_EntityID;
uniform vec3 lightColor;
uniform float strength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform vec3 u_ViewPosition;
uniform float u_Metallic;
uniform float u_Roughness;
uniform int u_HasAlbedoMap;
uniform int u_HasMetallicRoughnessMap;
uniform int u_HasNormalMap;
uniform sampler2D u_AlbedoMap;
uniform sampler2D u_MetallicRoughnessMap;
uniform sampler2D u_NormalMap;
uniform int u_UseIBL;
uniform float u_IblIntensity;
uniform samplerCube u_IrradianceMap;
uniform samplerCube u_PrefilterMap;
uniform sampler2D u_BrdfLut;
uniform sampler2D u_ShadowMap;
uniform int u_ShadowsEnabled;
uniform vec3 u_PointLightPosition;
uniform vec3 u_PointLightColor;
uniform float u_PointLightRange;
uniform samplerCube u_PointShadowMap;
uniform int u_PointShadowsEnabled;
uniform vec3 u_EmissiveFactor;
uniform int u_HasEmissiveMap;
uniform sampler2D u_EmissiveMap;
uniform int u_AlphaMode;
uniform float u_AlphaCutoff;

const float MAX_REFLECTION_LOD = 4.0;
const float PI = 3.14159265359;

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;
    float denom = NdotH2 * (a2 - 1.0) + 1.0;
    return a2 / (PI * denom * denom);
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    return NdotV / (NdotV * (1.0 - k) + k);
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    return GeometrySchlickGGX(NdotV, roughness) * GeometrySchlickGGX(NdotL, roughness);
}

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 FresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

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
    vec3 N;
    if (u_HasNormalMap != 0)
    {
        vec3 sampledNormal = texture(u_NormalMap, v_TexCoord).rgb * 2.0 - 1.0;
        N = normalize(v_TBN * sampledNormal);
    }
    else
    {
        N = normalize(v_Normal);
    }

    vec3 albedo = u_HasAlbedoMap != 0
        ? texture(u_AlbedoMap, v_TexCoord).rgb
        : vec3(1.0);
    albedo *= u_Color.rgb;

    float alpha = u_Color.a;
    if (u_HasAlbedoMap != 0)
        alpha *= texture(u_AlbedoMap, v_TexCoord).a;
    if (u_AlphaMode == 1 && alpha < u_AlphaCutoff)
        discard;

    float metallic = u_Metallic;
    float roughness = u_Roughness;
    float ao = 1.0;
    if (u_HasMetallicRoughnessMap != 0)
    {
        vec3 mr = texture(u_MetallicRoughnessMap, v_TexCoord).rgb;
        roughness *= mr.g;
        metallic *= mr.b;
        ao = mr.r;
    }
    roughness = clamp(roughness, 0.04, 1.0);
    metallic = clamp(metallic, 0.0, 1.0);

    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 L = normalize(-u_LightDirection);
    vec3 H = normalize(V + L);

    float NdotL = max(dot(N, L), 0.0);
    float NdotV = max(dot(N, V), 0.0);

    vec3 F0 = mix(vec3(0.04), albedo, metallic);
    float D = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3 specular = (D * G * F) / max(4.0 * NdotV * NdotL, 0.001);
    vec3 kd = (vec3(1.0) - F) * (1.0 - metallic);
    vec3 diffuse = kd * albedo / PI;

    // Metals have no diffuse ambient; without IBL use a small residual so they aren't pure black.
    vec3 ambient;
    if (u_UseIBL != 0)
    {
        vec3 R = reflect(-V, N);
        vec3 F_ibl = FresnelSchlickRoughness(NdotV, F0, roughness);
        vec3 kD_ibl = (vec3(1.0) - F_ibl) * (1.0 - metallic);
        vec3 irradiance = texture(u_IrradianceMap, N).rgb;
        vec3 prefiltered = textureLod(u_PrefilterMap, R, roughness * MAX_REFLECTION_LOD).rgb;
        vec3 diffuseIBL = kD_ibl * irradiance * albedo;
        vec2 brdf = texture(u_BrdfLut, vec2(NdotV, roughness)).rg;
        vec3 specularIBL = prefiltered * (F_ibl * brdf.x + brdf.y);
        float specOcclusion = clamp(
            pow(NdotV + ao, exp2(-16.0 * roughness - 1.0)) + ao - 1.0,
            0.0, 1.0);
        ambient = (diffuseIBL * ao + specularIBL * specOcclusion) * u_IblIntensity;
    }
    else
    {
        ambient = strength * lightColor * albedo * mix(1.0, 0.1, metallic);
        ambient *= ao;
    }
    vec3 radiance = u_LightColor * PI;
    float shadow = u_ShadowsEnabled != 0 ? ShadowCalculation(v_FragPosLightSpace, N, L) : 0.0;
    vec3 color = ambient + (1.0 - shadow) * (diffuse + specular) * radiance * NdotL;

    vec3 toPoint = u_PointLightPosition - v_FragPos;
    float pointDistance = length(toPoint);
    if (u_PointLightColor != vec3(0.0) && pointDistance < u_PointLightRange)
    {
        vec3 Lp = toPoint / max(pointDistance, 1e-4);
        float NdotLp = max(dot(N, Lp), 0.0);
        vec3 Hp = normalize(V + Lp);
        float Dp = DistributionGGX(N, Hp, roughness);
        float Gp = GeometrySmith(N, V, Lp, roughness);
        vec3 Fp = FresnelSchlick(max(dot(Hp, V), 0.0), F0);
        vec3 specP = (Dp * Gp * Fp) / max(4.0 * NdotV * NdotLp, 0.001);
        vec3 kdp = (vec3(1.0) - Fp) * (1.0 - metallic);
        vec3 diffP = kdp * albedo / PI;
        float attenuation = 1.0 / max(pointDistance * pointDistance, 1e-4);
        float pShadow = u_PointShadowsEnabled != 0 ? PointShadowCalculation(v_FragPos) : 0.0;
        color += (1.0 - pShadow) * (diffP + specP) * u_PointLightColor * PI * NdotLp * attenuation;
    }

    vec3 emissive = u_EmissiveFactor;
    if (u_HasEmissiveMap != 0)
        emissive *= texture(u_EmissiveMap, v_TexCoord).rgb;
    color += emissive;

    o_Color = vec4(color, alpha);
    o_EntityID = u_EntityID;
}
