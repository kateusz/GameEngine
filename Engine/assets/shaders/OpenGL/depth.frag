#version 410 core

in vec2 v_TexCoord;

uniform sampler2D u_DiffuseMap;
uniform vec2 u_DiffuseUvScale;
uniform int u_AlphaTest;
uniform float u_AlphaCutoff;

void main()
{
    if (u_AlphaTest != 0 && texture(u_DiffuseMap, v_TexCoord * u_DiffuseUvScale).a < u_AlphaCutoff)
        discard;
}
