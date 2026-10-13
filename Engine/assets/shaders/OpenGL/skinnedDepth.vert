#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 6) in ivec4 a_Joints;
layout(location = 7) in vec4 a_Weights;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;

layout(std140) uniform JointPalette
{
    mat4 u_Joints[128];
};

out vec2 v_TexCoord;

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
    v_TexCoord = a_TexCoord;
    vec4 skinned = vec4(a_Position, 1.0) * SkinMatrix();
    gl_Position = skinned * u_Model * u_ViewProjection;
}
