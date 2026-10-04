#version 330 core

layout(location = 0) out vec4 o_Color;

in vec3 v_Direction;

const float PI = 3.14159265359;
const float c_SourceResolution = 512.0;
const uint c_SampleCount = 1024u;

uniform samplerCube environmentMap;
uniform float u_Roughness;

float RadicalInverse_VdC(uint bits)
{
    bits = (bits << 16u) | (bits >> 16u);
    bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
    bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
    bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
    bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
    return float(bits) * 2.3283064365386963e-10;
}

vec2 Hammersley(uint i, uint N)
{
    return vec2(float(i) / float(N), RadicalInverse_VdC(i));
}

vec3 ImportanceSampleGGX(vec2 Xi, vec3 N, float roughness)
{
    float a = roughness * roughness;
    float phi = 2.0 * PI * Xi.x;
    float cosTheta = sqrt((1.0 - Xi.y) / (1.0 + (a * a - 1.0) * Xi.y));
    float sinTheta = sqrt(1.0 - cosTheta * cosTheta);
    vec3 H = vec3(cos(phi) * sinTheta, sin(phi) * sinTheta, cosTheta);
    vec3 up = abs(N.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(1.0, 0.0, 0.0);
    vec3 tangent = normalize(cross(up, N));
    vec3 bitangent = cross(N, tangent);
    return normalize(tangent * H.x + bitangent * H.y + N * H.z);
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float denom = (NdotH * NdotH) * (a2 - 1.0) + 1.0;
    return a2 / (PI * denom * denom);
}

void main()
{
    vec3 N = normalize(v_Direction);
    vec3 R = N;
    vec3 V = R;
    vec3 prefiltered = vec3(0.0);
    float totalWeight = 0.0;

    for (uint i = 0u; i < c_SampleCount; ++i)
    {
        vec2 Xi = Hammersley(i, c_SampleCount);
        vec3 H = ImportanceSampleGGX(Xi, N, u_Roughness);
        vec3 L = normalize(2.0 * dot(V, H) * H - V);
        float nDotL = max(dot(N, L), 0.0);
        if (nDotL > 0.0)
        {
            float nDotH = max(dot(N, H), 0.0);
            float hDotV = max(dot(H, V), 0.0001);
            float D = DistributionGGX(N, H, u_Roughness);
            float pdf = (D * nDotH / (4.0 * hDotV)) + 0.0001;
            float saTexel = 4.0 * PI / (6.0 * c_SourceResolution * c_SourceResolution);
            float saSample = 1.0 / (float(c_SampleCount) * pdf + 0.0001);
            float mip = u_Roughness == 0.0 ? 0.0 : 0.5 * log2(saSample / saTexel);
            prefiltered += textureLod(environmentMap, L, mip).rgb * nDotL;
            totalWeight += nDotL;
        }
    }

    o_Color = vec4(totalWeight > 0.0 ? prefiltered / totalWeight : vec3(0.0), 1.0);
}
