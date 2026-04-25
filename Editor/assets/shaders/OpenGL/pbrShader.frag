#version 330 core

const float PI = 3.14159265359;
const int MAX_POINT_LIGHTS = 16;

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in mat3 v_TBN;
flat in int v_EntityID;

// --- Material textures ---
uniform sampler2D u_BaseColor;          // sRGB, RGBA
uniform sampler2D u_MetallicRoughness;  // linear: G=roughness, B=metallic (glTF packed)
uniform sampler2D u_Normal;             // linear, tangent-space
uniform sampler2D u_AO;                 // linear, R
uniform sampler2D u_Emissive;           // sRGB, RGB

// --- Material factors ---
uniform vec4  u_BaseColorFactor;
uniform float u_MetallicFactor;
uniform float u_RoughnessFactor;
uniform vec3  u_EmissiveFactor;
uniform float u_NormalScale;
uniform float u_AoStrength;

uniform int u_HasBaseColor;
uniform int u_HasMetallicRoughness;
uniform int u_HasNormal;
uniform int u_HasAO;
uniform int u_HasEmissive;

// --- Lights ---
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

uniform vec3 u_ViewPosition;
uniform int  u_EntityID;

// --- Helpers ---
vec3 sRGBToLinear(vec3 c) { return pow(max(c, vec3(0.0)), vec3(2.2)); }

// --- Cook-Torrance BRDF sub-functions ---
float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a  = roughness * roughness;
    float a2 = a * a;
    float NdotH  = max(dot(N, H), 0.0);
    float denom  = NdotH * NdotH * (a2 - 1.0) + 1.0;
    return a2 / (PI * denom * denom);
}

float GeometrySchlickGGX(float NdotX, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    return NdotX / (NdotX * (1.0 - k) + k);
}

float GeometrySmith(float NdotV, float NdotL, float roughness)
{
    return GeometrySchlickGGX(NdotV, roughness) * GeometrySchlickGGX(NdotL, roughness);
}

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

// Returns reflected radiance for a single punctual light.
vec3 ComputeLight(vec3 L, vec3 N, vec3 V, vec3 radiance, vec3 albedo, vec3 F0, float roughness, float metallic)
{
    vec3  H     = normalize(L + V);
    float NdotV = max(dot(N, V), 1e-4);
    float NdotL = max(dot(N, L), 0.0);
    if (NdotL == 0.0) return vec3(0.0);

    vec3  F   = FresnelSchlick(max(dot(H, V), 0.0), F0);
    float NDF = DistributionGGX(N, H, roughness);
    float G   = GeometrySmith(NdotV, NdotL, roughness);

    vec3 specular = (NDF * G * F) / max(4.0 * NdotV * NdotL, 1e-4);
    vec3 kD = (vec3(1.0) - F) * (1.0 - metallic);

    return (kD * albedo / PI + specular) * radiance * NdotL;
}

void main()
{
    // --- Sample BaseColor ---
    vec4 baseColorSample = u_HasBaseColor != 0 ? texture(u_BaseColor, v_TexCoord) : vec4(1.0);
    vec3 baseColor = sRGBToLinear(baseColorSample.rgb) * sRGBToLinear(u_BaseColorFactor.rgb);
    float alpha    = baseColorSample.a * u_BaseColorFactor.a;

    // --- Sample MetallicRoughness (G=roughness, B=metallic per glTF spec) ---
    vec3  mr        = u_HasMetallicRoughness != 0 ? texture(u_MetallicRoughness, v_TexCoord).rgb : vec3(0.0, 1.0, 0.0);
    float roughness = clamp(mr.g * u_RoughnessFactor, 0.045, 1.0);
    float metallic  = clamp(mr.b * u_MetallicFactor,  0.0,   1.0);

    // --- AO ---
    float ao = u_HasAO != 0 ? mix(1.0, texture(u_AO, v_TexCoord).r, u_AoStrength) : 1.0;

    // --- Normal ---
    vec3 N;
    if (u_HasNormal != 0)
    {
        vec3 nSample = texture(u_Normal, v_TexCoord).rgb * 2.0 - 1.0;
        nSample.xy  *= u_NormalScale;
        N = normalize(v_TBN * nSample);
    }
    else
    {
        N = normalize(v_Normal);
    }

    vec3 V  = normalize(u_ViewPosition - v_FragPos);
    vec3 F0 = mix(vec3(0.04), baseColor, metallic);

    vec3 Lo = vec3(0.0);

    // --- Directional light ---
    if (u_DirectionalLightEnabled != 0)
    {
        vec3 L       = normalize(-u_DirectionalLightDirection);
        vec3 radiance = sRGBToLinear(u_DirectionalLightColor) * u_DirectionalLightStrength;
        Lo += ComputeLight(L, N, V, radiance, baseColor, F0, roughness, metallic);
    }

    // --- Point lights ---
    int pointCount = min(u_PointLightCount, MAX_POINT_LIGHTS);
    for (int i = 0; i < pointCount; i++)
    {
        vec3  toLight    = u_PointLightPositions[i] - v_FragPos;
        float dist       = length(toLight);
        vec3  L          = toLight / max(dist, 1e-4);
        float attenuation = 1.0 / (1.0 + dist * dist);
        vec3  radiance    = sRGBToLinear(u_PointLightColors[i]) * u_PointLightIntensities[i] * attenuation;
        Lo += ComputeLight(L, N, V, radiance, baseColor, F0, roughness, metallic);
    }

    // --- Constant ambient (AO modulates) ---
    vec3 ambient = u_AmbientLightEnabled != 0
        ? sRGBToLinear(u_AmbientLightColor) * u_AmbientLightStrength * baseColor * ao
        : vec3(0.0);

    // --- Emissive ---
    vec3 emissive = vec3(0.0);
    if (u_HasEmissive != 0)
        emissive = sRGBToLinear(texture(u_Emissive, v_TexCoord).rgb);
    emissive *= u_EmissiveFactor;

    // Output stays in linear HDR; ACES + gamma encode happens in the tone-mapper pass.
    o_Color    = vec4(ambient + Lo + emissive, alpha);
    o_EntityID = u_EntityID;
}
