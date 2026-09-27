#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 3) in vec3 a_Tangent;
layout(location = 4) in vec3 a_Bitangent;
layout(location = 6) in mat4 a_InstanceModel;
layout(location = 10) in mat4 a_InstanceNormal;
layout(location = 14) in int a_InstanceEntityId;

uniform mat4 u_ViewProjection;

out vec3 v_FragPos;
out vec3 v_Normal;
out vec2 v_TexCoord;
out mat3 v_TBN;
flat out int v_EntityID;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * a_InstanceModel;
    v_FragPos  = worldPos.xyz;
    v_Normal   = normalize(a_Normal * mat3(a_InstanceNormal));
    v_TexCoord = a_TexCoord;
    v_EntityID = a_InstanceEntityId;

    vec3 T = normalize(a_Tangent * mat3(a_InstanceNormal));
    vec3 N = v_Normal;
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T);
    v_TBN = mat3(T, B, N);

    gl_Position = worldPos * u_ViewProjection;
}
