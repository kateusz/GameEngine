#version 330 core

layout(location = 0) out vec4 color;

in vec4 v_Color;
in vec2 v_TexCoord;

//uniform vec4 u_Color;
//uniform float u_TilingFactor;
//uniform sampler2D u_Texture;

void main()
{
    // color = texture(u_Texture, v_TexCoord * u_TilingFactor) * u_Color;
    color = v_Color;
}