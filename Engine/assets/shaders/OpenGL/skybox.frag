#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_Direction;

uniform samplerCube u_Skybox;

void main()
{
    o_Color = vec4(textureLod(u_Skybox, v_Direction, 0.0).rgb, 1.0);
    o_EntityID = -1;
}