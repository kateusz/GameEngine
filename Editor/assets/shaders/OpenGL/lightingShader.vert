#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 3) in vec3 a_Tangent;
layout(location = 4) in vec3 a_Bitangent;
layout(location = 5) in vec4 a_BoneIndexF;
layout(location = 6) in vec4 a_BoneWeight;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform mat4 u_NormalMatrix;
// N=100 must match SkeletonReader.MaxBones
uniform mat4 u_BoneMatrices[100];

out vec3 v_FragPos;
out vec3 v_Normal;
out vec2 v_TexCoord;
out mat3 v_TBN;

// System.Numerics Vector4.Transform after transpose upload: row-vector v*M =
// (dot(v, col0), …). GLSL `v * mat4` uses matrix rows — wrong for animated bones; identity hides it.
vec4 MulRowVectorMatrix(vec4 v, mat4 m)
{
    return vec4(dot(v, m[0]), dot(v, m[1]), dot(v, m[2]), dot(v, m[3]));
}

ivec4 BoneIndices()
{
    // Float bone indices (macOS int attribs are unreliable). Clamp — OOB is undefined.
    return clamp(ivec4(a_BoneIndexF + 0.5), ivec4(0), ivec4(99));
}

vec3 SkinPosition()
{
    float weightSum = a_BoneWeight.x + a_BoneWeight.y + a_BoneWeight.z + a_BoneWeight.w;
    if (weightSum < 1e-5)
        return a_Position;

    vec4 p = vec4(a_Position, 1.0);
    vec4 pos = vec4(0.0);
    pos += MulRowVectorMatrix(p, u_BoneMatrices[BoneIndices().x]) * a_BoneWeight.x;
    pos += MulRowVectorMatrix(p, u_BoneMatrices[BoneIndices().y]) * a_BoneWeight.y;
    pos += MulRowVectorMatrix(p, u_BoneMatrices[BoneIndices().z]) * a_BoneWeight.z;
    pos += MulRowVectorMatrix(p, u_BoneMatrices[BoneIndices().w]) * a_BoneWeight.w;
    return pos.xyz;
}

vec3 SkinDirection(vec3 dir)
{
    float weightSum = a_BoneWeight.x + a_BoneWeight.y + a_BoneWeight.z + a_BoneWeight.w;
    if (weightSum < 1e-5)
        return dir;

    vec4 d = vec4(dir, 0.0);
    vec4 outDir = vec4(0.0);
    outDir += MulRowVectorMatrix(d, u_BoneMatrices[BoneIndices().x]) * a_BoneWeight.x;
    outDir += MulRowVectorMatrix(d, u_BoneMatrices[BoneIndices().y]) * a_BoneWeight.y;
    outDir += MulRowVectorMatrix(d, u_BoneMatrices[BoneIndices().z]) * a_BoneWeight.z;
    outDir += MulRowVectorMatrix(d, u_BoneMatrices[BoneIndices().w]) * a_BoneWeight.w;
    return outDir.xyz;
}

void main()
{
    vec3 skinnedPos = SkinPosition();
    vec3 skinnedNormal = SkinDirection(a_Normal);
    vec3 skinnedTangent = SkinDirection(a_Tangent);
    vec3 skinnedBitangent = SkinDirection(a_Bitangent);

    vec4 worldPos = vec4(skinnedPos, 1.0) * u_Model;
    v_FragPos  = worldPos.xyz;
    v_Normal   = normalize(skinnedNormal * mat3(u_NormalMatrix));
    v_TexCoord = a_TexCoord;

    vec3 T = normalize(skinnedTangent * mat3(u_NormalMatrix));
    vec3 B = skinnedBitangent * mat3(u_NormalMatrix);
    vec3 N = v_Normal;
    // Re-orthogonalize T against N; keep Assimp B for mirrored-UV handedness.
    T = normalize(T - dot(T, N) * N);
    if (dot(B, B) < 1e-8)
        B = cross(N, T);
    else
    {
        B = normalize(B);
        B = normalize(B - dot(B, N) * N - dot(B, T) * T);
    }
    v_TBN = mat3(T, B, N);

    gl_Position = worldPos * u_ViewProjection;
}
