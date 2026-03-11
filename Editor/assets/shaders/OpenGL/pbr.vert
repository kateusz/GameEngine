#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 3) in vec3 a_Tangent;
layout(location = 4) in vec3 a_Bitangent;
layout(location = 5) in int a_EntityID;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform mat4 u_NormalMatrix;
uniform mat4 u_LightSpaceMatrix;

out vec3 v_WorldPos;
out vec3 v_Normal;
out vec2 v_TexCoord;
out vec4 v_LightSpacePos;
out mat3 v_TBN;
flat out int v_EntityID;

void main()
{
    // macOS: System.Numerics row-major matrices require row-vector multiplication
    vec4 worldPos = vec4(a_Position, 1.0) * u_Model;
    v_WorldPos = worldPos.xyz;
    v_TexCoord = a_TexCoord;
    v_EntityID = a_EntityID;

    mat3 normalMatrix = mat3(u_NormalMatrix);
    v_Normal = normalize(a_Normal * normalMatrix);

    // TBN matrix for normal mapping
    vec3 T = normalize(a_Tangent * normalMatrix);
    vec3 B = normalize(a_Bitangent * normalMatrix);
    vec3 N = v_Normal;
    // Re-orthogonalize using Gram-Schmidt
    T = normalize(T - dot(T, N) * N);
    B = cross(N, T);
    v_TBN = mat3(T, B, N);

    // Shadow mapping: transform to light space
    v_LightSpacePos = worldPos * u_LightSpaceMatrix;

    gl_Position = worldPos * u_ViewProjection;
}
