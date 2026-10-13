#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 3) in vec3 a_Tangent;
layout(location = 4) in vec3 a_Bitangent;
layout(location = 6) in ivec4 a_Joints;
layout(location = 7) in vec4 a_Weights;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform mat4 u_NormalMatrix;
uniform int u_EntityID;

layout(std140) uniform JointPalette
{
    mat4 u_Joints[128];
};

out vec3 v_FragPos;
out vec3 v_Normal;
out vec2 v_TexCoord;
out mat3 v_TBN;
flat out int v_EntityID;

mat4 SkinMatrix()
{
    float sum = a_Weights.x + a_Weights.y + a_Weights.z + a_Weights.w;
    if (sum <= 0.0)
        return mat4(1.0);
    return u_Joints[a_Joints.x] * a_Weights.x
         + u_Joints[a_Joints.y] * a_Weights.y
         + u_Joints[a_Joints.z] * a_Weights.z
         + u_Joints[a_Joints.w] * a_Weights.w;
}

void main()
{
    mat4 skin = SkinMatrix();
    vec4 skinned = vec4(a_Position, 1.0) * skin;
    vec4 worldPos = skinned * u_Model;
    v_FragPos = worldPos.xyz;
    vec3 skinnedNormal = a_Normal * mat3(skin);
    vec3 skinnedTangent = a_Tangent * mat3(skin);
    v_Normal = normalize(skinnedNormal * mat3(u_NormalMatrix));
    v_TexCoord = a_TexCoord;
    v_EntityID = u_EntityID;

    vec3 T = normalize(skinnedTangent * mat3(u_NormalMatrix));
    vec3 N = v_Normal;
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T);
    v_TBN = mat3(T, B, N);

    gl_Position = worldPos * u_ViewProjection;
}
