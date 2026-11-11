#version 410 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in mat3 v_TBN;
flat in int v_EntityID;

#include "frame.glsl"
uniform vec4 u_Color;
uniform vec4 u_MaterialFactors; // Metallic, roughness, ambient occlusion, alpha cutoff.
uniform vec3 u_Emissive;
uniform samplerCube u_Irradiance;
uniform samplerCube u_Prefilter;
uniform sampler2D u_BrdfLut;
uniform vec3 u_BaseColor;
uniform int u_MaterialFlags;
uniform sampler2D u_DiffuseMap;
uniform sampler2D u_MetallicRoughnessMap;
uniform sampler2D u_NormalMap;
uniform sampler2D u_OcclusionMap;
uniform vec2 u_DiffuseUvScale;
uniform vec2 u_MetallicRoughnessUvScale;
uniform vec2 u_NormalUvScale;
uniform vec2 u_OcclusionUvScale;

const int c_MaxPointLights = 8;
const int c_DiffuseMap = 1;
const int c_MetallicRoughnessMap = 2;
const int c_NormalMap = 4;
const int c_ReconstructNormalZ = 8;
const int c_OcclusionMap = 16;
const int c_AlphaCutout = 32;
const float c_PointEpsilon = 0.0001;

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

        vec3 toLight = u_PointLightPositions[i].xyz - fragPos;
        float dist = length(toLight);
        if (dist >= u_PointLightIntensityRange[i].y)
            continue;

        vec3 radiance = u_PointLightColors[i].xyz * u_PointLightIntensityRange[i].x;
        if (dist < c_PointEpsilon)
        {
            sum += radiance * albedo;
            continue;
        }

        float remaining = 1.0 - dist / u_PointLightIntensityRange[i].y;
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

vec3 ImageBasedLight(vec3 N, vec3 V, vec3 albedo, float metallic, float roughness, float ao)
{
    vec3 F0 = mix(vec3(c_DielectricF0), albedo, metallic);
    float nDotV = max(dot(N, V), 0.0);
    vec3 F = FresnelSchlickRoughness(nDotV, F0, roughness);
    vec3 kD = (1.0 - F) * (1.0 - metallic);
    vec3 diffuse = texture(u_Irradiance, N).rgb * albedo;
    vec3 R = reflect(-V, N);
    vec3 prefiltered = textureLod(u_Prefilter, R, roughness * 4.0).rgb;
    vec2 brdf = texture(u_BrdfLut, vec2(nDotV, roughness)).rg;
    return (kD * diffuse + prefiltered * (F * brdf.x + brdf.y)) * ao;
}

void main()
{
    vec3 norm;
    if ((u_MaterialFlags & c_NormalMap) != 0) {
        vec3 nts = texture(u_NormalMap, v_TexCoord * u_NormalUvScale).rgb * 2.0 - 1.0;
        if ((u_MaterialFlags & c_ReconstructNormalZ) != 0) {
            nts.z = sqrt(max(1.0 - nts.x * nts.x - nts.y * nts.y, 0.0));
        }
        norm = normalize(v_TBN * nts);
    } else {
        norm = normalize(v_Normal);
    }

    vec4 texel = (u_MaterialFlags & c_DiffuseMap) != 0 ? texture(u_DiffuseMap, v_TexCoord * u_DiffuseUvScale) : vec4(1.0);
    if ((u_MaterialFlags & c_AlphaCutout) != 0 && texel.a < u_MaterialFactors.w)
        discard;
    vec3 albedo = texel.rgb * u_BaseColor * u_Color.rgb;
    vec3 mr = (u_MaterialFlags & c_MetallicRoughnessMap) != 0
        ? texture(u_MetallicRoughnessMap, v_TexCoord * u_MetallicRoughnessUvScale).rgb
        : vec3(1.0);
    float metallic = clamp(mr.b * u_MaterialFactors.x, 0.0, 1.0);
    float roughness = max(clamp(mr.g * u_MaterialFactors.y, 0.0, 1.0), c_MinRoughness);
    float aoSample = (u_MaterialFlags & c_OcclusionMap) != 0 ? texture(u_OcclusionMap, v_TexCoord * u_OcclusionUvScale).r : 1.0;
    float ao = clamp(aoSample * u_MaterialFactors.z, 0.0, 1.0);

    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 L = normalize(-u_LightDirection);
    float shadow = DirectionalShadow(v_FragPos);
    vec3 sun = CookTorrance(norm, V, L, u_LightColor, albedo, metallic, roughness) * shadow;
    vec3 lamps = PointLights(norm, V, v_FragPos, albedo, metallic, roughness);
    vec3 ambient = u_Ibl != 0
        ? ImageBasedLight(norm, V, albedo, metallic, roughness, ao)
        : u_AmbientStrength * u_AmbientColor * albedo * ao;
    o_Color = vec4(ambient + sun + lamps + u_Emissive, u_Color.a);
    o_EntityID = v_EntityID;
}
