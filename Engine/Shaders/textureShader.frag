#version 330 core

out vec4 outputColor;
in vec2 v_TexCoord;

uniform sampler2D u_Texture;
uniform vec4 u_Color;
uniform float u_TilingFactor;

void main()
{
    outputColor = texture(u_Texture, v_TexCoord * u_TilingFactor) * u_Color;
}