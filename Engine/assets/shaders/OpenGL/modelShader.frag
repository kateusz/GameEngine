#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_FragPos;
in vec3 v_Normal;
in vec2 v_TexCoord;
in mat3 v_TBN;
flat in int v_EntityID;

uniform vec4 u_Color;
uniform vec3 u_AmbientColor;
uniform float u_AmbientStrength;
uniform vec3 u_LightDirection;
uniform vec3 u_LightColor;
uniform vec3 u_ViewPosition;
uniform float u_Metallic;
uniform float u_Roughness;
uniform float u_Ao;
uniform int u_Ibl;
uniform samplerCube u_Irradiance;
uniform samplerCube u_Prefilter;
uniform sampler2D u_BrdfLut;
uniform vec3 u_BaseColor;
uniform int u_HasDiffuseMap;
uniform int u_AlphaTest;
uniform float u_AlphaCutoff;
uniform int u_HasMetallicRoughnessMap;
uniform int u_HasNormalMap;
uniform int u_HasOcclusionMap;
uniform sampler2D u_DiffuseMap;
uniform sampler2D u_MetallicRoughnessMap;
uniform sampler2D u_NormalMap;
uniform sampler2D u_OcclusionMap;

const int c_MaxPointLights = 8;
const float c_PointEpsilon = 0.0001;

uniform int u_PointLightCount;
uniform vec3 u_PointLightPositions[c_MaxPointLights];
uniform vec3 u_PointLightColors[c_MaxPointLights];
uniform float u_PointLightIntensities[c_MaxPointLights];
uniform float u_PointLightRanges[c_MaxPointLights];

uniform int u_PointShadowsEnabled[c_MaxPointLights];
uniform samplerCube u_PointShadowMaps[c_MaxPointLights];

uniform mat4 u_LightViewProjection;
uniform sampler2D u_ShadowMap;
uniform int u_ShadowsEnabled;

const float c_ShadowBias = 0.002;
const float PI = 3.14159265359;
const float c_MinRoughness = 0.045;
const float c_DielectricF0 = 0.04;
const float c_SpecularEpsilon = 0.0001;

float DirectionalShadow(vec3 fragPos)
{
    if (u_ShadowsEnabled == 0)
        return 1.0;

    vec4 clipPos = vec4(fragPos, 1.0) * u_LightViewProjection;
    vec3 ndc = clipPos.xyz / clipPos.w;
    vec2 uv = ndc.xy * 0.5 + 0.5;
    float current = ndc.z * 0.5 + 0.5;

    // bilinear PCF; integer-offset PCF is constant per texel → stairs
    vec2 mapSize = vec2(textureSize(u_ShadowMap, 0));
    vec2 texelSize = 1.0 / mapSize;
    vec2 texelUv = uv * mapSize - 0.5;
    vec2 f = fract(texelUv);
    vec2 origin = (floor(texelUv) + 0.5) * texelSize;

    float shadow = 0.0;
    for (int x = 0; x <= 1; ++x)
    {
        for (int y = 0; y <= 1; ++y)
        {
            float closest = texture(u_ShadowMap, origin + vec2(x, y) * texelSize).r;
            float lit = current - c_ShadowBias > closest ? 0.0 : 1.0;
            float wx = x == 0 ? 1.0 - f.x : f.x;
            float wy = y == 0 ? 1.0 - f.y : f.y;
            shadow += lit * wx * wy;
        }
    }
    return shadow;
}

const float c_PointShadowBias = 0.05;

float PointShadow(int i, vec3 fragPos)
{
    if (u_PointShadowsEnabled[i] == 0)
        return 1.0;

    vec3 toFrag = fragPos - u_PointLightPositions[i];
    float dist = length(toFrag);
    float range = u_PointLightRanges[i];
    float current = dist / range;
    float bias = c_PointShadowBias / range;
    vec3 direction = toFrag / dist;
    vec3 helper = abs(direction.y) > 0.99 ? vec3(1.0, 0.0, 0.0) : vec3(0.0, 1.0, 0.0);
    vec3 tangent = normalize(cross(helper, direction));
    vec3 bitangent = cross(direction, tangent);
    float step = dist * 3.14159265 / 512.0;
    float shadow = 0.0;
    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            vec3 sampleDir = toFrag + (tangent * float(x) + bitangent * float(y)) * step;
            float closest = texture(u_PointShadowMaps[i], sampleDir).r;
            shadow += current - bias > closest ? 0.0 : 1.0;
        }
    }

    return shadow / 9.0;
}

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

vec3 Encode(vec3 color)
{
    color = color / (color + vec3(1.0));
    return pow(color, vec3(1.0 / 2.2));
}

void main()
{
    vec3 norm = u_HasNormalMap != 0
        ? normalize(v_TBN * (texture(u_NormalMap, v_TexCoord).rgb * 2.0 - 1.0))
        : normalize(v_Normal);

    vec4 texel = u_HasDiffuseMap != 0 ? texture(u_DiffuseMap, v_TexCoord) : vec4(1.0);
    if (u_AlphaTest != 0 && texel.a < u_AlphaCutoff)
        discard;
    vec3 albedo = texel.rgb * u_BaseColor * u_Color.rgb;
    vec3 mr = u_HasMetallicRoughnessMap != 0
        ? texture(u_MetallicRoughnessMap, v_TexCoord).rgb
        : vec3(1.0);
    float metallic = clamp(mr.b * u_Metallic, 0.0, 1.0);
    float roughness = max(clamp(mr.g * u_Roughness, 0.0, 1.0), c_MinRoughness);
    float aoSample = u_HasOcclusionMap != 0 ? texture(u_OcclusionMap, v_TexCoord).r : 1.0;
    float ao = clamp(aoSample * u_Ao, 0.0, 1.0);

    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 L = normalize(-u_LightDirection);
    float shadow = DirectionalShadow(v_FragPos);
    vec3 sun = CookTorrance(norm, V, L, u_LightColor, albedo, metallic, roughness) * shadow;
    vec3 lamps = PointLights(norm, V, v_FragPos, albedo, metallic, roughness);
    vec3 ambient = u_Ibl != 0
        ? ImageBasedLight(norm, V, albedo, metallic, roughness, ao)
        : u_AmbientStrength * u_AmbientColor * albedo * ao;
    o_Color = vec4(Encode(ambient + sun + lamps), u_Color.a);
    o_EntityID = v_EntityID;
}
