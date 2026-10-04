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
uniform sampler2D u_Texture;
uniform int u_UseTexture;
uniform float u_Metallic;
uniform float u_Roughness;
uniform float u_Ao;
uniform sampler2D u_Ssao;
uniform float u_SsaoStrength;
uniform vec3 u_Emissive;
uniform int u_Ibl;
uniform samplerCube u_Irradiance;
uniform samplerCube u_Prefilter;
#ifdef USE_BRDF_LUT
uniform sampler2D u_BrdfLut;
#endif

const int c_MaxPointLights = 8;
const float c_PointEpsilon = 0.0001;

uniform int u_PointLightCount;
uniform vec3 u_PointLightPositions[c_MaxPointLights];
uniform vec3 u_PointLightColors[c_MaxPointLights];
uniform float u_PointLightIntensities[c_MaxPointLights];
uniform float u_PointLightRanges[c_MaxPointLights];

uniform int u_PointShadowsEnabled[c_MaxPointLights];
uniform samplerCubeShadow u_PointShadowMaps[c_MaxPointLights];

#include "shadow.glsl"

const float PI = 3.14159265359;
const float c_MinRoughness = 0.045;
const float c_DielectricF0 = 0.04;
const float c_SpecularEpsilon = 0.0001;

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float denom = (NdotH * NdotH) * (a2 - 1.0) + 1.0;
    return a2 / (PI * denom * denom);
}

float GeometrySchlickGGX(float Ndot, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    return Ndot / (Ndot * (1.0 - k) + k);
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    return GeometrySchlickGGX(max(dot(N, V), 0.0), roughness)
         * GeometrySchlickGGX(max(dot(N, L), 0.0), roughness);
}

vec3 CookTorrance(vec3 N, vec3 V, vec3 L, vec3 radiance, vec3 albedo, float metallic, float roughness)
{
    float NdotL = max(dot(N, L), 0.0);
    vec3 H = normalize(V + L);
    vec3 F0 = mix(vec3(c_DielectricF0), albedo, metallic);
    float D = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);
    vec3 specular = (D * G * F) / (4.0 * max(dot(N, V), 0.0) * NdotL + c_SpecularEpsilon);
    vec3 kD = (vec3(1.0) - F) * (1.0 - metallic);
    return (kD * albedo / PI + specular) * radiance * NdotL;
}

vec3 PointLights(vec3 N, vec3 V, vec3 fragPos, vec3 albedo, float metallic, float roughness)
{
    vec3 sum = vec3(0.0);
    for (int i = 0; i < c_MaxPointLights; i++)
    {
        if (i >= u_PointLightCount)
            break;

        vec3 toLight = u_PointLightPositions[i] - fragPos;
        float dist = length(toLight);
        if (dist >= u_PointLightRanges[i])
            continue;

        vec3 radiance = u_PointLightColors[i] * u_PointLightIntensities[i];
        if (dist < c_PointEpsilon)
        {
            sum += radiance * albedo;
            continue;
        }

        float remaining = 1.0 - dist / u_PointLightRanges[i];
        radiance *= remaining * remaining;
        sum += CookTorrance(N, V, toLight / dist, radiance, albedo, metallic, roughness)
            * PointShadow(i, fragPos);
    }
    return sum;
}

vec3 FresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

#ifndef USE_BRDF_LUT
vec2 EnvBrdfApprox(float nDotV, float roughness)
{
    vec4 c0 = vec4(-1.0, -0.0275, -0.572, 0.022);
    vec4 c1 = vec4(1.0, 0.0425, 1.04, -0.04);
    vec4 r = roughness * c0 + c1;
    float a004 = min(r.x * r.x, exp2(-9.28 * nDotV)) * r.x + r.y;
    return vec2(-1.04, 1.04) * a004 + r.zw;
}
#endif

vec3 ImageBasedLight(vec3 N, vec3 V, vec3 albedo, float metallic, float roughness, float ao)
{
    vec3 F0 = mix(vec3(c_DielectricF0), albedo, metallic);
    float nDotV = max(dot(N, V), 0.0);
    vec3 F = FresnelSchlickRoughness(nDotV, F0, roughness);
    vec3 kD = (1.0 - F) * (1.0 - metallic);
    vec3 diffuse = texture(u_Irradiance, N).rgb * albedo;
    vec3 R = reflect(-V, N);
    vec3 prefiltered = textureLod(u_Prefilter, R, roughness * 4.0).rgb;
#ifdef USE_BRDF_LUT
    vec2 brdf = texture(u_BrdfLut, vec2(nDotV, roughness)).rg;
#else
    vec2 brdf = EnvBrdfApprox(nDotV, roughness);
#endif
    return (kD * diffuse + prefiltered * (F * brdf.x + brdf.y)) * ao;
}

void main()
{
    vec3 albedo = (u_UseTexture == 1 ? texture(u_Texture, v_TexCoord).rgb : vec3(1.0)) * u_Color.rgb;
    float metallic = clamp(u_Metallic, 0.0, 1.0);
    float roughness = max(clamp(u_Roughness, 0.0, 1.0), c_MinRoughness);
    float ao = clamp(u_Ao, 0.0, 1.0);

    vec3 N = normalize(v_Normal);
    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 L = normalize(-u_LightDirection);
    float shadow = DirectionalShadow(v_FragPos);
    vec3 sun = CookTorrance(N, V, L, u_LightColor, albedo, metallic, roughness) * shadow;
    vec3 lamps = PointLights(N, V, v_FragPos, albedo, metallic, roughness);
    vec3 ambient = u_Ibl != 0
        ? ImageBasedLight(N, V, albedo, metallic, roughness, ao)
        : u_AmbientStrength * u_AmbientColor * albedo * ao;
    float ssao = texture(u_Ssao, gl_FragCoord.xy / vec2(textureSize(u_Ssao, 0))).r;
    ambient *= mix(1.0, ssao, u_SsaoStrength);
    o_Color = vec4(ambient + sun + lamps + u_Emissive, u_Color.a);
    o_EntityID = u_EntityID;
}
