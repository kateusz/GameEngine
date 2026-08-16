#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_Dir;

uniform samplerCube u_Skybox;
uniform float u_Intensity;

void main()
{
    o_Color = vec4(texture(u_Skybox, normalize(v_Dir)).rgb * u_Intensity, 1.0);
    o_EntityID = -1;
}
