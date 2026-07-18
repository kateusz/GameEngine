#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 3) in vec3 a_Tangent;
layout(location = 4) in vec3 a_Bitangent;
layout(location = 5) in int  a_EntityID;
layout(location = 6) in ivec4 a_BoneIds;
layout(location = 7) in vec4  a_BoneWeights;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform mat4 u_NormalMatrix;
uniform int  u_HasBones;
uniform mat4 u_BoneMatrices[32];

out vec3 v_FragPos;
out vec3 v_Normal;
out vec2 v_TexCoord;
out mat3 v_TBN;
flat out int v_EntityID;

void main()
{
    vec4 localPos = vec4(a_Position, 1.0);
    vec3 localNormal = a_Normal;
    vec3 localTangent = a_Tangent;
    vec3 localBitangent = a_Bitangent;

    if (u_HasBones == 1)
    {
        // Clamp: OOB bone indices cause native GPU/driver faults (CORDBG_E_TARGET_INCONSISTENT).
        ivec4 boneIds = clamp(a_BoneIds, ivec4(0), ivec4(31));
        mat4 skin =
            a_BoneWeights.x * u_BoneMatrices[boneIds.x] +
            a_BoneWeights.y * u_BoneMatrices[boneIds.y] +
            a_BoneWeights.z * u_BoneMatrices[boneIds.z] +
            a_BoneWeights.w * u_BoneMatrices[boneIds.w];
        localPos = localPos * skin;
        mat3 skin3 = mat3(skin);
        localNormal = localNormal * skin3;
        localTangent = localTangent * skin3;
        localBitangent = localBitangent * skin3;
    }

    vec4 worldPos = localPos * u_Model;
    v_FragPos  = worldPos.xyz;
    v_Normal   = normalize(localNormal * mat3(u_NormalMatrix));
    v_TexCoord = a_TexCoord;
    v_EntityID = a_EntityID;

    vec3 T = normalize(localTangent * mat3(u_NormalMatrix));
    vec3 B = localBitangent * mat3(u_NormalMatrix);
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
