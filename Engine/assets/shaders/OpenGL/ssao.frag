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

vec3 ViewFromZ(vec2 uv, float viewZ)
{
    vec2 ndc = uv * 2.0 - 1.0;
    vec4 clip = vec4(0.0, 0.0, viewZ, 1.0) * u_Projection;
    float ndcZ = clip.z / clip.w;
    vec4 viewH = vec4(ndc, ndcZ, 1.0) * u_InverseProjection;
    return viewH.xyz / viewH.w;
}

void main()
{
    vec4 packed = texture(u_Normal, v_TexCoord);
    // Empty texels are 0. A surface in front of the camera has a negative view Z.
    if (packed.a >= 0.0)
    {
        FragColor = 1.0;
        return;
    }

    vec3 fragView = ViewFromZ(v_TexCoord, packed.a);
    vec3 normal = normalize(packed.rgb);
    if (dot(normal, fragView) > 0.0)
        normal = -normal;

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
        if (offset.x < 0.0 || offset.x > 1.0 || offset.y < 0.0 || offset.y > 1.0)
            continue;

        float sampleZ = texture(u_Normal, offset.xy).a;
        if (sampleZ >= 0.0)
            continue;

        float weight = smoothstep(0.0, 1.0, u_Radius / abs(fragView.z - sampleZ));
        occlusion += (sampleZ >= samplePos.z + u_Bias ? 1.0 : 0.0) * weight;
    }

    // 64 taps. A corner only blocks part of the hemisphere, so scale past 1/64 or the contact stays invisible next to the sun.
    FragColor = clamp(1.0 - occlusion / 16.0, 0.0, 1.0);
}
