#version 330 core

const float PI = 3.14159265359;
const int MAX_POINT_LIGHTS = 8;
const int MAX_SPOT_LIGHTS = 4;

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int o_EntityID;

in vec3 v_WorldPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in vec4 v_LightSpacePos;
in mat3 v_TBN;
flat in int v_EntityID;

// Material textures
uniform sampler2D u_AlbedoMap;
uniform sampler2D u_NormalMap;
uniform sampler2D u_MetallicMap;
uniform sampler2D u_RoughnessMap;
uniform sampler2D u_AOMap;
uniform sampler2D u_EmissiveMap;
uniform sampler2D u_ShadowMap;

// IBL textures
uniform samplerCube u_IrradianceMap;
uniform samplerCube u_PrefilterMap;
uniform sampler2D u_BrdfLUT;
uniform int u_HasIBL;

// Material scalar fallbacks
uniform vec4 u_AlbedoColor;
uniform float u_Metallic;
uniform float u_Roughness;
uniform float u_AO;
uniform vec3 u_EmissiveColor;
uniform float u_EmissiveIntensity;

// Texture flags
uniform int u_HasAlbedoMap;
uniform int u_HasNormalMap;
uniform int u_HasMetallicMap;
uniform int u_HasRoughnessMap;
uniform int u_HasAOMap;
uniform int u_HasEmissiveMap;
uniform int u_HasShadowMap;

// Camera
uniform vec3 u_ViewPosition;

// Scene lighting controls
uniform float u_Exposure;
uniform float u_AmbientIntensity;
uniform vec3 u_AmbientColor;
uniform float u_IBLIntensity;

// Directional light
uniform vec3 u_DirLightDirection;
uniform vec3 u_DirLightColor;
uniform float u_DirLightIntensity;
uniform int u_HasDirLight;

// Point lights
uniform int u_NumPointLights;
uniform vec3 u_PointLightPositions[MAX_POINT_LIGHTS];
uniform vec3 u_PointLightColors[MAX_POINT_LIGHTS];
uniform float u_PointLightIntensities[MAX_POINT_LIGHTS];
uniform float u_PointLightRanges[MAX_POINT_LIGHTS];

// Spot lights
uniform int u_NumSpotLights;
uniform vec3 u_SpotLightPositions[MAX_SPOT_LIGHTS];
uniform vec3 u_SpotLightDirections[MAX_SPOT_LIGHTS];
uniform vec3 u_SpotLightColors[MAX_SPOT_LIGHTS];
uniform float u_SpotLightIntensities[MAX_SPOT_LIGHTS];
uniform float u_SpotLightRanges[MAX_SPOT_LIGHTS];
uniform float u_SpotLightInnerCones[MAX_SPOT_LIGHTS];
uniform float u_SpotLightOuterCones[MAX_SPOT_LIGHTS];

// ============== PBR Functions ==============

// Normal Distribution Function (GGX/Trowbridge-Reitz)
float D_GGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return a2 / max(denom, 0.0000001);
}

// Geometry Function (Smith's Schlick-GGX)
float G_SchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;
    float denom = NdotV * (1.0 - k) + k;
    return NdotV / max(denom, 0.0000001);
}

float G_Smith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    return G_SchlickGGX(NdotV, roughness) * G_SchlickGGX(NdotL, roughness);
}

// Fresnel Function (Schlick approximation)
vec3 F_Schlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 F_SchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

// Point light attenuation
float attenuate(float distance, float range)
{
    float att = clamp(1.0 - (distance * distance) / (range * range), 0.0, 1.0);
    return att * att;
}

// Shadow calculation with PCF
float calculateShadow(vec4 lightSpacePos, vec3 normal, vec3 lightDir)
{
    if (u_HasShadowMap == 0)
        return 0.0;

    // Perspective divide
    vec3 projCoords = lightSpacePos.xyz / lightSpacePos.w;
    projCoords = projCoords * 0.5 + 0.5;

    // Outside shadow map range
    if (projCoords.z > 1.0 || projCoords.x < 0.0 || projCoords.x > 1.0 ||
        projCoords.y < 0.0 || projCoords.y > 1.0)
        return 0.0;

    float currentDepth = projCoords.z;

    // Slope-based bias to reduce shadow acne
    float bias = max(0.005 * (1.0 - dot(normal, lightDir)), 0.001);

    // PCF 5x5
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(u_ShadowMap, 0);
    for (int x = -2; x <= 2; ++x)
    {
        for (int y = -2; y <= 2; ++y)
        {
            float pcfDepth = texture(u_ShadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;
        }
    }
    shadow /= 25.0;

    return shadow;
}

// ============== Lighting Calculation ==============

vec3 calculatePBRLighting(vec3 L, vec3 V, vec3 N, vec3 albedo, float metallic,
                          float roughness, vec3 F0, vec3 lightColor, float lightIntensity)
{
    vec3 H = normalize(V + L);
    float NdotL = max(dot(N, L), 0.0);

    // Cook-Torrance BRDF
    float D = D_GGX(N, H, roughness);
    float G = G_Smith(N, V, L, roughness);
    vec3 F = F_Schlick(max(dot(H, V), 0.0), F0);

    vec3 numerator = D * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * NdotL + 0.0001;
    vec3 specular = numerator / denominator;

    // Energy conservation: diffuse and specular cannot exceed 1.0
    vec3 kS = F;
    vec3 kD = vec3(1.0) - kS;
    kD *= 1.0 - metallic; // Metals have no diffuse

    vec3 radiance = lightColor * lightIntensity;
    return (kD * albedo / PI + specular) * radiance * NdotL;
}

void main()
{
    // Sample material properties
    // Note: albedo textures use GL_SRGB8_ALPHA8 format, so GPU automatically
    // converts sRGB to linear on texture fetch - no manual pow(2.2) needed
    vec4 albedo4;
    if (u_HasAlbedoMap == 1)
        albedo4 = texture(u_AlbedoMap, v_TexCoord) * u_AlbedoColor;
    else
        albedo4 = u_AlbedoColor;

    // Alpha test for transparency
    if (albedo4.a < 0.1)
        discard;

    vec3 albedo = albedo4.rgb;

    float metallic;
    if (u_HasMetallicMap == 1)
        metallic = texture(u_MetallicMap, v_TexCoord).r;
    else
        metallic = u_Metallic;

    float roughness;
    if (u_HasRoughnessMap == 1)
        roughness = texture(u_RoughnessMap, v_TexCoord).r;
    else
        roughness = u_Roughness;
    roughness = max(roughness, 0.045);

    float ao;
    if (u_HasAOMap == 1)
        ao = texture(u_AOMap, v_TexCoord).r;
    else
        ao = u_AO;

    // Normal mapping
    vec3 Ngeom = normalize(v_Normal); // Geometric normal (smooth, no normal map)
    vec3 N;
    if (u_HasNormalMap == 1)
    {
        vec3 tangentNormal = texture(u_NormalMap, v_TexCoord).rgb * 2.0 - 1.0;
        N = normalize(v_TBN * tangentNormal);
    }
    else
    {
        N = Ngeom;
    }

    vec3 V = normalize(u_ViewPosition - v_WorldPos);
    float NdotV = max(dot(N, V), 0.0);

    // F0: reflectance at normal incidence (0.04 for dielectrics, albedo for metals)
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    vec3 Lo = vec3(0.0);

    // Directional light
    if (u_HasDirLight == 1)
    {
        vec3 L = normalize(-u_DirLightDirection);
        float shadow = calculateShadow(v_LightSpacePos, N, L);
        vec3 contribution = calculatePBRLighting(L, V, N, albedo, metallic, roughness,
                                                  F0, u_DirLightColor, u_DirLightIntensity);
        Lo += contribution * (1.0 - shadow);
    }

    // Point lights
    for (int i = 0; i < u_NumPointLights; ++i)
    {
        vec3 lightVec = u_PointLightPositions[i] - v_WorldPos;
        float distance = length(lightVec);
        vec3 L = normalize(lightVec);
        float att = attenuate(distance, u_PointLightRanges[i]);

        Lo += calculatePBRLighting(L, V, N, albedo, metallic, roughness,
                                    F0, u_PointLightColors[i], u_PointLightIntensities[i]) * att;
    }

    // Spot lights
    for (int i = 0; i < u_NumSpotLights; ++i)
    {
        vec3 lightVec = u_SpotLightPositions[i] - v_WorldPos;
        float distance = length(lightVec);
        vec3 L = normalize(lightVec);
        float att = attenuate(distance, u_SpotLightRanges[i]);

        // Spot cone
        float theta = dot(L, normalize(-u_SpotLightDirections[i]));
        float epsilon = u_SpotLightInnerCones[i] - u_SpotLightOuterCones[i];
        float spotIntensity = clamp((theta - u_SpotLightOuterCones[i]) / max(epsilon, 0.0001), 0.0, 1.0);

        Lo += calculatePBRLighting(L, V, N, albedo, metallic, roughness,
                                    F0, u_SpotLightColors[i], u_SpotLightIntensities[i]) * att * spotIntensity;
    }

    // Ambient / IBL lighting
    vec3 ambient;
    float ambientStr = u_AmbientIntensity > 0.0 ? u_AmbientIntensity : 0.5;
    vec3 ambientColor = length(u_AmbientColor) > 0.0 ? u_AmbientColor : vec3(1.0);

    if (u_HasIBL == 1)
    {
        // Use geometric normal for IBL Fresnel to avoid blue fringing at edges
        // Normal-mapped N can face away from camera at silhouettes, causing NdotV->0
        // which maximizes Fresnel and shows blue sky reflections on all edges
        float NdotVgeom = max(dot(Ngeom, V), 0.0);

        vec3 F = F_SchlickRoughness(NdotVgeom, F0, roughness);
        vec3 kS = F;
        vec3 kD = (1.0 - kS) * (1.0 - metallic);

        // Diffuse IBL: use geometric normal to avoid per-pixel noise from normal maps
        vec3 irradiance = texture(u_IrradianceMap, Ngeom).rgb;
        vec3 diffuseIBL = irradiance * albedo;

        // Specular IBL - use geometric normal for smooth reflections
        vec3 R = reflect(-V, Ngeom);
        const float MAX_REFLECTION_LOD = 4.0;
        vec3 prefilteredColor = textureLod(u_PrefilterMap, R, roughness * MAX_REFLECTION_LOD).rgb;
        vec2 brdf = texture(u_BrdfLUT, vec2(NdotVgeom, roughness)).rg;
        vec3 specularIBL = prefilteredColor * (F * brdf.x + brdf.y);
        // Fade out specular IBL at grazing angles to prevent blue sky fringing
        float horizonFade = smoothstep(0.0, 0.3, NdotVgeom);
        specularIBL *= horizonFade;

        float iblStr = u_IBLIntensity > 0.0 ? u_IBLIntensity : 1.0;
        vec3 iblAmbient = (kD * diffuseIBL + specularIBL) * iblStr;

        // Blend IBL with neutral flat ambient to prevent environment color cast
        vec3 flatAmbient = ambientColor * ambientStr * albedo;
        ambient = mix(flatAmbient, iblAmbient, 0.7) * ao;
    }
    else
    {
        // Fallback flat ambient
        ambient = ambientColor * ambientStr * albedo * ao;
    }

    // Emissive (emissive textures also use sRGB format, GPU converts automatically)
    vec3 emissive = vec3(0.0);
    if (u_HasEmissiveMap == 1)
        emissive = texture(u_EmissiveMap, v_TexCoord).rgb * u_EmissiveIntensity;
    else if (u_EmissiveIntensity > 0.0)
        emissive = u_EmissiveColor * u_EmissiveIntensity;

    vec3 color = ambient + Lo + emissive;

    // HDR exposure
    float exposure = u_Exposure > 0.0 ? u_Exposure : 1.0;
    color *= exposure;

    // ACES filmic tone mapping using ACEScg matrices (Stephen Hill fit)
    // Preserves chromaticity better than per-channel Narkowicz approximation
    // Input transform: sRGB -> ACEScg
    mat3 ACESInputMat = mat3(
        0.59719, 0.07600, 0.02840,
        0.35458, 0.90834, 0.13383,
        0.04823, 0.01566, 0.83777
    );
    // Output transform: ACEScg -> sRGB
    mat3 ACESOutputMat = mat3(
         1.60475, -0.10208, -0.00327,
        -0.53108,  1.10813, -0.07276,
        -0.07367, -0.00605,  1.07602
    );
    color = ACESInputMat * color;
    // RRT and ODT fit
    vec3 a = color * (color + 0.0245786) - 0.000090537;
    vec3 b = color * (0.983729 * color + 0.4329510) + 0.238081;
    color = a / b;
    color = ACESOutputMat * color;
    color = clamp(color, 0.0, 1.0);

    // Gamma correction (linear to sRGB)
    color = pow(color, vec3(1.0 / 2.2));

    o_Color = vec4(color, albedo4.a);
    o_EntityID = v_EntityID;
}
