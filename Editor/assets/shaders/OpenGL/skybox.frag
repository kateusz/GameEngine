#version 330 core

out vec4 o_Color;

in vec3 v_TexCoords;

uniform samplerCube u_Skybox;

void main()
{
    o_Color = texture(u_Skybox, v_TexCoords);
}