#version 330 core
layout(location = 0) in vec3 a_Position;
layout(location = 5) in vec4 a_BoneIndexF;
layout(location = 6) in vec4 a_Weights;

uniform mat4 u_Model;
uniform mat4 u_ShadowMatrix;
uniform mat4 u_BoneMatrices[100];
uniform int u_Skinned;

out vec4 FragPos;

vec4 MulRowVectorMatrix(vec4 v, mat4 m)
{
    return vec4(dot(v, m[0]), dot(v, m[1]), dot(v, m[2]), dot(v, m[3]));
}

mat4 SkinMatrix()
{
    float weightSum = a_Weights.x + a_Weights.y + a_Weights.z + a_Weights.w;
    if (u_Skinned == 0 || weightSum < 1e-5)
        return mat4(1.0);

    ivec4 bi = clamp(ivec4(a_BoneIndexF + 0.5), ivec4(0), ivec4(99));
    return u_BoneMatrices[bi.x] * a_Weights.x
         + u_BoneMatrices[bi.y] * a_Weights.y
         + u_BoneMatrices[bi.z] * a_Weights.z
         + u_BoneMatrices[bi.w] * a_Weights.w;
}

void main()
{
    float weightSum = a_Weights.x + a_Weights.y + a_Weights.z + a_Weights.w;
    vec4 localPos = weightSum < 1e-5
        ? vec4(a_Position, 1.0)
        : MulRowVectorMatrix(vec4(a_Position, 1.0), SkinMatrix());
    FragPos = localPos * u_Model;
    gl_Position = FragPos * u_ShadowMatrix;
}
