#version 330 core

out float FragColor;

in vec2 v_TexCoord;

uniform sampler2D u_Normal;
uniform sampler2D u_Depth;
uniform sampler2D u_Noise;
uniform vec3 u_Samples[64];
uniform mat4 u_Projection;
uniform mat4 u_InverseProjection;
uniform float u_Radius;
uniform float u_Bias;
uniform vec2 u_NoiseScale;

void main()
{
    float windowDepth = texture(u_Depth, v_TexCoord).r;
    if (windowDepth >= 1.0)
    {
        FragColor = 1.0;
        return;
    }

    vec2 ndcXy = v_TexCoord * 2.0 - 1.0;
    float ndcZ = windowDepth * 2.0 - 1.0;
    vec4 viewH = vec4(ndcXy, ndcZ, 1.0) * u_InverseProjection;
    vec3 fragView = viewH.xyz / viewH.w;

    vec3 normal = normalize(texture(u_Normal, v_TexCoord).rgb);
    vec3 randomVec = texture(u_Noise, v_TexCoord * u_NoiseScale).rgb * 2.0 - 1.0;
    vec3 tangent = randomVec - normal * dot(randomVec, normal);
    if (dot(tangent, tangent) < 1e-4)
        tangent = abs(normal.z) < 0.999 ? vec3(-normal.y, normal.x, 0.0) : vec3(0.0, -normal.z, normal.y);
    tangent = normalize(tangent);
    vec3 bitangent = cross(normal, tangent);
    mat3 TBN = mat3(tangent, bitangent, normal);

    float occlusion = 0.0;
    for (int i = 0; i < 64; ++i)
    {
        vec3 samplePos = fragView + (TBN * u_Samples[i]) * u_Radius;
        vec4 offset = vec4(samplePos, 1.0) * u_Projection;
        offset.xy /= offset.w;
        offset.xy = offset.xy * 0.5 + 0.5;

        float sampleWindow = texture(u_Depth, offset.xy).r;
        vec2 sampleNdc = offset.xy * 2.0 - 1.0;
        vec4 sampleH = vec4(sampleNdc, sampleWindow * 2.0 - 1.0, 1.0) * u_InverseProjection;
        float sampleZ = sampleH.z / sampleH.w;

        float weight = smoothstep(0.0, 1.0, u_Radius / abs(fragView.z - sampleZ));
        occlusion += (sampleZ >= samplePos.z + u_Bias ? 1.0 : 0.0) * weight;
    }

    FragColor = 1.0 - occlusion / 64.0;
}
