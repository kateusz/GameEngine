#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 3) in vec3 a_Tangent;
layout(location = 4) in vec3 a_Bitangent;
layout(location = 5) in int  a_EntityID;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform mat4 u_NormalMatrix;

out vec3 v_FragPos;
out vec3 v_Normal;
out vec2 v_TexCoord;
out mat3 v_TBN;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * u_Model;
    v_FragPos  = worldPos.xyz;
    v_Normal   = normalize(a_Normal * mat3(u_NormalMatrix));
    v_TexCoord = a_TexCoord;

    vec3 T = normalize(a_Tangent * mat3(u_NormalMatrix));
    vec3 N = v_Normal;
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T);
    v_TBN = mat3(T, B, N);

    gl_Position = worldPos * u_ViewProjection;
}
