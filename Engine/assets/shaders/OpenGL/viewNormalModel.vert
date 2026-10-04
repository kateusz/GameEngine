#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 6) in mat4 a_InstanceModel;
layout(location = 10) in mat4 a_InstanceNormal;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;
uniform mat4 u_NormalMatrix;
uniform mat4 u_View;
uniform int u_Instanced;

out vec3 v_ViewNormal;
out vec2 v_TexCoord;

void main()
{
    mat4 model = u_Instanced != 0 ? a_InstanceModel : u_Model;
    mat4 normalMat = u_Instanced != 0 ? a_InstanceNormal : u_NormalMatrix;
    vec3 worldNormal = normalize(a_Normal * mat3(normalMat));
    v_ViewNormal = normalize(worldNormal * mat3(u_View));
    v_TexCoord = a_TexCoord;
    gl_Position = vec4(a_Position, 1.0) * model * u_ViewProjection;
}
