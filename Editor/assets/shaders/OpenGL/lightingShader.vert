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

// One weighted bone matrix, then apply once to position / directions.
mat4 SkinMatrix()
{
    float weightSum = a_BoneWeight.x + a_BoneWeight.y + a_BoneWeight.z + a_BoneWeight.w;
    if (weightSum < 1e-5)
        return mat4(1.0);

    ivec4 bi = BoneIndices();
    return u_BoneMatrices[bi.x] * a_BoneWeight.x
         + u_BoneMatrices[bi.y] * a_BoneWeight.y
         + u_BoneMatrices[bi.z] * a_BoneWeight.z
         + u_BoneMatrices[bi.w] * a_BoneWeight.w;
}

void main()
{
    mat4 skin = SkinMatrix();
    float weightSum = a_BoneWeight.x + a_BoneWeight.y + a_BoneWeight.z + a_BoneWeight.w;
    vec3 skinnedPos = weightSum < 1e-5
        ? a_Position
        : MulRowVectorMatrix(vec4(a_Position, 1.0), skin).xyz;
    vec3 skinnedNormal = weightSum < 1e-5
        ? a_Normal
        : MulRowVectorMatrix(vec4(a_Normal, 0.0), skin).xyz;
    vec3 skinnedTangent = weightSum < 1e-5
        ? a_Tangent
        : MulRowVectorMatrix(vec4(a_Tangent, 0.0), skin).xyz;
    vec3 skinnedBitangent = weightSum < 1e-5
        ? a_Bitangent
        : MulRowVectorMatrix(vec4(a_Bitangent, 0.0), skin).xyz;

    vec4 worldPos = MulRowVectorMatrix(vec4(skinnedPos, 1.0), u_Model);
    v_FragPos  = worldPos.xyz;
    v_Normal   = normalize(MulRowVectorMatrix(vec4(skinnedNormal, 0.0), u_NormalMatrix).xyz);
    v_TexCoord = a_TexCoord;

    vec3 T = normalize(MulRowVectorMatrix(vec4(skinnedTangent, 0.0), u_NormalMatrix).xyz);
    vec3 B = MulRowVectorMatrix(vec4(skinnedBitangent, 0.0), u_NormalMatrix).xyz;
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

    gl_Position = MulRowVectorMatrix(worldPos, u_ViewProjection);
}
