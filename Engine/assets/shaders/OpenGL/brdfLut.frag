#version 330 core

layout(location = 0) out vec4 o_Color;

in vec2 v_TexCoord;

const float PI = 3.14159265359;
const uint c_SampleCount = 1024u;

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

float GeometrySchlickGGX(float nDot, float roughness)
{
    float a = roughness;
    float k = (a * a) / 2.0;
    return nDot / (nDot * (1.0 - k) + k);
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    return GeometrySchlickGGX(max(dot(N, V), 0.0), roughness)
         * GeometrySchlickGGX(max(dot(N, L), 0.0), roughness);
}

vec2 IntegrateBRDF(float nDotV, float roughness)
{
    vec3 V = vec3(sqrt(1.0 - nDotV * nDotV), 0.0, nDotV);
    float A = 0.0;
    float B = 0.0;
    vec3 N = vec3(0.0, 0.0, 1.0);
    for (uint i = 0u; i < c_SampleCount; ++i)
    {
        vec2 Xi = Hammersley(i, c_SampleCount);
        vec3 H = ImportanceSampleGGX(Xi, N, roughness);
        vec3 L = normalize(2.0 * dot(V, H) * H - V);
        float nDotL = max(L.z, 0.0);
        float nDotH = max(H.z, 0.0);
        float vDotH = max(dot(V, H), 0.0);
        if (nDotL > 0.0)
        {
            float G = GeometrySmith(N, V, L, roughness);
            float vis = (G * vDotH) / (nDotH * nDotV);
            float fc = pow(1.0 - vDotH, 5.0);
            A += (1.0 - fc) * vis;
            B += fc * vis;
        }
    }
    return vec2(A, B) / float(c_SampleCount);
}

void main()
{
    vec2 integrated = IntegrateBRDF(v_TexCoord.x, v_TexCoord.y);
    o_Color = vec4(integrated, 0.0, 1.0);
}
