#version 330 core

in vec2 v_TexCoord;

uniform vec4 u_Color;
uniform int u_HasAlbedoMap;
uniform int u_AlphaMode;
uniform float u_AlphaCutoff;
uniform sampler2D u_AlbedoMap;

void main()
{
    if (u_AlphaMode == 0)
        return;

    float alpha = u_Color.a;
    if (u_HasAlbedoMap != 0)
        alpha *= texture(u_AlbedoMap, v_TexCoord).a;

    if (alpha < u_AlphaCutoff)
        discard;
}
